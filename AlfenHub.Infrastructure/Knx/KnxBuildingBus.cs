using AlfenHub.Application.Abstractions;
using AlfenHub.Application.Charging.Commands;
using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlfenHub.Infrastructure.Knx;

/// <summary>
/// KNX building-bus adapter implementing the application's <see cref="IBuildingBus"/> port. Owns the
/// Falcon <see cref="KnxBus"/> connection, the capability→group-address mapping, the byte encoding
/// (incl. the on-the-wire byte-order reversal) and the diff buffer of last-sent values.
/// <para>
/// Outbound: projects changed charger readings onto the bus. Inbound: a KNX <c>ValueRead</c> is
/// answered from the buffer; a <c>ValueWrite</c> is translated into a
/// <see cref="SetSocketMaxCurrentCommand"/> dispatched via MediatR.
/// </para>
/// <para>
/// Disposed by the DI container on host shutdown (after the polling worker has stopped):
/// <see cref="DisposeAsync"/> closes the tunnelling connection so the KNX/IP interface frees its
/// tunnel slot immediately instead of holding it until its heartbeat times out.
/// </para>
/// </summary>
internal sealed class KnxBuildingBus : IBuildingBus, IAsyncDisposable
{
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<KnxBuildingBus> _logger;
    private readonly ISender _sender;
    private readonly KnxOptions _options;

    private readonly Dictionary<string, KnxValue> _capabilityKnxValueMapping;
    private readonly Dictionary<GroupAddress, string> _readGroupAddressCapabilityMapping;
    private readonly Dictionary<GroupAddress, string> _writeGroupAddressCapabilityMapping;
    private readonly object _bufferLock = new();

    // Cancelled on dispose; scopes the inbound (bus → charger) message handling.
    private readonly CancellationTokenSource _shutdown = new();

    private KnxBus? _bus;
    private int _disposed;

    public KnxBuildingBus(ILogger<KnxBuildingBus> logger, ISender sender, IOptions<KnxOptions> options)
    {
        _logger = logger;
        _sender = sender;
        _options = options.Value;

        _capabilityKnxValueMapping = _options.GetReadGroupAddressesFromOptions()
            .ToDictionary(mapping => mapping.Key, mapping => new KnxValue(GroupAddress.Parse(mapping.Value)));
        _readGroupAddressCapabilityMapping = _options.GetReadGroupAddressesFromOptions()
            .ToDictionary(mapping => GroupAddress.Parse(mapping.Value), mapping => mapping.Key);
        _writeGroupAddressCapabilityMapping = _options.GetWriteGroupAddressesFromOptions()
            .ToDictionary(mapping => GroupAddress.Parse(mapping.Value), mapping => mapping.Key);
    }

    public bool IsEnabled => _options.Enabled;

    public async Task PublishAsync(Charger charger, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (_bus is null)
        {
            _logger.LogError("Something went wrong after connecting to knx client");
            return;
        }

        var changedValues = UpdateBuffer(charger);
        await SendValuesAsync(changedValues, asResponse: false, cancellationToken);
    }

    private List<KnxValue> UpdateBuffer(Charger charger)
    {
        lock (_bufferLock)
        {
            return KnxReadingEncoder.Encode(charger)
                .Select(reading => UpdateValue(reading.Capability, reading.Value))
                .Where(value => value is not null)
                .ToList()!;
        }
    }

    private KnxValue? UpdateValue(string capability, byte[] value)
    {
        if (!_capabilityKnxValueMapping.TryGetValue(capability, out var knxValue))
        {
            return null;
        }

        if (knxValue.Value is not null && knxValue.Value.SequenceEqual(value))
        {
            return null;
        }

        knxValue.Value = value;
        return knxValue;
    }

    private async Task SendValuesAsync(IEnumerable<KnxValue> values, bool asResponse, CancellationToken cancellationToken)
    {
        if (_bus?.ConnectionState != BusConnectionState.Connected)
        {
            await EnsureConnectedAsync(cancellationToken);
        }

        // Local copy: DisposeAsync may clear the field concurrently.
        var bus = _bus;
        if (bus is null)
        {
            _logger.LogError("Something went wrong after connecting to knx client");
            return;
        }

        foreach (var knxValue in values)
        {
            if (knxValue.Value is null)
            {
                continue;
            }

            using var writeCancellationToken = new CancellationTokenSource();

            // KnxReadingEncoder already produces wire-order (big-endian) DPT bytes.
            var value = new GroupValue(knxValue.Value);

            // Answering a GroupValueRead must be an A_GroupValue_Response; a spontaneous publish is an
            // A_GroupValue_Write. A requester waiting for a Response can retransmit / time out otherwise.
            var sendTask = asResponse
                ? bus.RespondGroupValueAsync(knxValue.Address, value, MessagePriority.Low, writeCancellationToken.Token)
                : bus.WriteGroupValueAsync(knxValue.Address, value, MessagePriority.Low, writeCancellationToken.Token);

            await Task.WhenAny(
                sendTask,
                Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken));

            await writeCancellationToken.CancelAsync();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);

        if (_bus?.ConnectionState == BusConnectionState.Connected)
        {
            return;
        }

        // Release a previous (dropped / never-connected) bus before replacing it, otherwise its
        // socket and event subscription leak on every reconnect.
        var stale = Interlocked.Exchange(ref _bus, null);
        if (stale is not null)
        {
            await CloseBusAsync(stale);
        }

        var bus = new KnxBus(new IpTunnelingConnectorParameters(_options.Host, _options.Port));
        bus.GroupMessageReceived += OnGroupMessageReceived;
        _bus = bus;

        // Disposed while we were closing the stale bus: don't leave a fresh connection behind.
        if (Volatile.Read(ref _disposed) == 1)
        {
            if (Interlocked.CompareExchange(ref _bus, null, bus) == bus)
            {
                await CloseBusAsync(bus);
            }

            throw new ObjectDisposedException(nameof(KnxBuildingBus));
        }

        await bus.ConnectAsync(cancellationToken);

        if (bus.ConnectionState == BusConnectionState.Connected)
        {
            _logger.LogInformation("Connected to {Host} at port: {Port}", _options.Host, _options.Port);
        }
        else
        {
            _logger.LogError("Something went wrong when trying to connect to {Host} at port: {Port}", _options.Host, _options.Port);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        await _shutdown.CancelAsync();

        var bus = Interlocked.Exchange(ref _bus, null);
        if (bus is not null)
        {
            await CloseBusAsync(bus);
            _logger.LogInformation("Disconnected from {Host} at port: {Port}", _options.Host, _options.Port);
        }

        _shutdown.Dispose();
    }

    /// <summary>
    /// Unsubscribes and disposes a bus; disposing closes the tunnel (DISCONNECT_REQUEST). Bounded by
    /// <see cref="CloseTimeout"/> so an unreachable interface cannot stall shutdown, and never throws.
    /// </summary>
    private async Task CloseBusAsync(KnxBus bus)
    {
        bus.GroupMessageReceived -= OnGroupMessageReceived;

        try
        {
            await bus.DisposeAsync().AsTask().WaitAsync(CloseTimeout);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not cleanly close the KNX connection to {Host}", _options.Host);
        }
    }

    // async void event handler: must not let an exception escape (it would crash the process).
    private async void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        try
        {
            if (Volatile.Read(ref _disposed) == 1)
            {
                return;
            }

            await ProcessGroupMessageReceivedAsync(e, _shutdown.Token);
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
        }
    }

    private async Task ProcessGroupMessageReceivedAsync(GroupEventArgs e, CancellationToken cancellationToken)
    {
        switch (e.EventType)
        {
            case GroupEventType.ValueRead:
                if (!_readGroupAddressCapabilityMapping.TryGetValue(e.DestinationAddress, out var readCapability))
                {
                    return;
                }

                KnxValue? bufferedValue;
                lock (_bufferLock)
                {
                    _capabilityKnxValueMapping.TryGetValue(readCapability, out bufferedValue);
                }

                if (bufferedValue?.Value is null)
                {
                    return;
                }

                await SendValuesAsync([bufferedValue], asResponse: true, cancellationToken);
                return;

            case GroupEventType.ValueWrite:
                if (!_writeGroupAddressCapabilityMapping.TryGetValue(e.DestinationAddress, out var writeCapability))
                {
                    break;
                }

                try
                {
                    await ProcessInboundWriteAsync(writeCapability, e.Value.Value, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Message}", ex.Message);
                }

                break;

            default:
                _logger.LogTrace("Message type'{Type}' not implemented", e.EventType);
                break;
        }
    }

    private async Task ProcessInboundWriteAsync(string capability, byte[] value, CancellationToken cancellationToken)
    {
        switch (capability)
        {
            case KnxCapabilities.SlaveMaxCurrent:
                var amperes = BitConverter.ToSingle(value.Reverse().ToArray());
                await _sender.Send(
                    new SetSocketMaxCurrentCommand(new SocketId(1), new ElectricCurrent(amperes)),
                    cancellationToken);
                break;

            default:
                _logger.LogWarning("Writing parameter '{Parameter}' not implemented", capability);
                break;
        }
    }
}

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
/// </summary>
internal sealed class KnxBuildingBus : IBuildingBus
{
    private readonly ILogger<KnxBuildingBus> _logger;
    private readonly ISender _sender;
    private readonly KnxOptions _options;

    private readonly Dictionary<string, KnxValue> _capabilityKnxValueMapping;
    private readonly Dictionary<GroupAddress, string> _readGroupAddressCapabilityMapping;
    private readonly Dictionary<GroupAddress, string> _writeGroupAddressCapabilityMapping;
    private readonly object _bufferLock = new();

    private KnxBus? _bus;

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
        await SendValuesAsync(changedValues, cancellationToken);
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

    private async Task SendValuesAsync(IEnumerable<KnxValue> values, CancellationToken cancellationToken)
    {
        if (_bus?.ConnectionState != BusConnectionState.Connected)
        {
            await EnsureConnectedAsync(cancellationToken);
        }

        if (_bus is null)
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

            var writeCancellationToken = new CancellationTokenSource();

            await Task.WhenAny(
                _bus.WriteGroupValueAsync(
                    knxValue.Address,
                    // KnxReadingEncoder already produces wire-order (big-endian) DPT bytes.
                    new GroupValue(knxValue.Value),
                    MessagePriority.Low,
                    writeCancellationToken.Token),
                Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken));

            await writeCancellationToken.CancelAsync();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_bus?.ConnectionState == BusConnectionState.Connected)
        {
            return;
        }

        _bus = new KnxBus(new IpTunnelingConnectorParameters(_options.Host, _options.Port));
        _bus.GroupMessageReceived += async (_, args) =>
        {
            await ProcessGroupMessageReceivedAsync(args, cancellationToken);
        };

        await _bus.ConnectAsync(cancellationToken);

        if (_bus.ConnectionState == BusConnectionState.Connected)
        {
            _logger.LogInformation("Connected to {Host} at port: {Port}", _options.Host, _options.Port);
        }
        else
        {
            _logger.LogError("Something went wrong when trying to connect to {Host} at port: {Port}", _options.Host, _options.Port);
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

                await SendValuesAsync([bufferedValue], cancellationToken);
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

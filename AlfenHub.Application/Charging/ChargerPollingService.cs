using AlfenHub.Application.Control;
using AlfenHub.Application.Events;
using AlfenHub.Domain.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlfenHub.Application.Charging;

/// <summary>
/// The polling loop (the orchestration that used to live inside <c>AlfenModbusClient</c>). Each
/// cycle it re-asserts any pending setpoints to the charger, reads a fresh snapshot, and dispatches
/// the resulting domain events. All device I/O goes through <see cref="IChargerGateway"/>; this
/// service has no knowledge of Modbus or KNX.
/// </summary>
public sealed class ChargerPollingService
{
    private readonly IChargerGateway _gateway;
    private readonly IChargerControlBuffer _controlBuffer;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ChargerPollingOptions _options;
    private readonly ILogger<ChargerPollingService> _logger;

    public ChargerPollingService(
        IChargerGateway gateway,
        IChargerControlBuffer controlBuffer,
        IDomainEventDispatcher domainEventDispatcher,
        IOptions<ChargerPollingOptions> options,
        ILogger<ChargerPollingService> logger)
    {
        _gateway = gateway;
        _controlBuffer = controlBuffer;
        _domainEventDispatcher = domainEventDispatcher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_options.PollInterval, cancellationToken);

                try
                {
                    // Re-assert pending setpoints first (mirrors the previous write-before-read order).
                    foreach (var setpoint in _controlBuffer.GetPending())
                    {
                        await _gateway.WriteMaxCurrentAsync(setpoint.SocketId, setpoint.MaxCurrent, cancellationToken);
                    }

                    var charger = await _gateway.GetAsync(cancellationToken);

                    await _domainEventDispatcher.DispatchAsync(charger.ReleaseEvents(), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{Message}", e.Message);
                }
            }
        }, cancellationToken);
    }
}

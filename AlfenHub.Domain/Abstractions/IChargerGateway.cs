using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Domain.Abstractions;

/// <summary>
/// Port to the physical charger. The repository for the <see cref="Charger"/> aggregate: it loads
/// the current state and persists the one thing we can write — the max-current setpoint.
/// Implemented by an infrastructure adapter (Alfen Modbus).
/// </summary>
public interface IChargerGateway
{
    Task<Charger> GetAsync(CancellationToken cancellationToken);

    Task WriteMaxCurrentAsync(SocketId socketId, ElectricCurrent maxCurrent, CancellationToken cancellationToken);
}

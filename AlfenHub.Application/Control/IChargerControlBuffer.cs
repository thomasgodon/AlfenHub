using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Application.Control;

/// <summary>
/// Holds the latest desired control values for the charger, decoupling the inbound (building-bus)
/// side from the polling loop. This replaces the old shared-mutable singleton bridge.
/// <para>
/// Setpoints are <b>retained</b> (latest value per socket wins) rather than drained: the polling
/// loop re-asserts them on every cycle, because an Alfen charger falls back to its safe current
/// once the Modbus max-current validity time elapses.
/// </para>
/// </summary>
public interface IChargerControlBuffer
{
    void SetMaxCurrent(SocketId socketId, ElectricCurrent maxCurrent);

    IReadOnlyCollection<SocketMaxCurrent> GetPending();
}

/// <summary>A desired max-current setpoint for a specific socket.</summary>
public readonly record struct SocketMaxCurrent(SocketId SocketId, ElectricCurrent MaxCurrent);

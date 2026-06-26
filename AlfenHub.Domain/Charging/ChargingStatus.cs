using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Domain.Charging;

/// <summary>
/// Immutable snapshot of a socket's charging status and current transaction state.
/// </summary>
public sealed record ChargingStatus
{
    public required ushort Availability { get; init; }
    public required Mode3State Mode3State { get; init; }
    public required ElectricCurrent ActualAppliedMaxCurrent { get; init; }
    public required MaxCurrentSetpoint SlaveMaxCurrent { get; init; }
    public required ElectricCurrent ActiveLoadBalancingSafeCurrent { get; init; }
    public required ushort ModbusSlaveReceivedSetPointAccountedFor { get; init; }
    public required ushort ChargePhases { get; init; }
}

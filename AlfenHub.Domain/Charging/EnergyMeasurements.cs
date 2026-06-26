using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Domain.Charging;

/// <summary>
/// Immutable snapshot of a socket's energy meter readings. Modelled as a value object: two
/// snapshots with the same readings are considered equal.
/// </summary>
public sealed record EnergyMeasurements
{
    public required ushort MeterState { get; init; }
    public required Voltage VoltageL1N { get; init; }
    public required Voltage VoltageL2N { get; init; }
    public required Voltage VoltageL3N { get; init; }
    public required Voltage VoltageL1L2 { get; init; }
    public required Voltage VoltageL2L3 { get; init; }
    public required Voltage VoltageL3L1 { get; init; }
    public required ElectricCurrent CurrentN { get; init; }
    public required ElectricCurrent CurrentSum { get; init; }
    public required PowerFactor PowerFactorSum { get; init; }
    public required Frequency Frequency { get; init; }
    public required Power RealPowerSum { get; init; }
    public required Power ApparentPowerSum { get; init; }
    public required Power ReactivePowerSum { get; init; }
    public required Energy RealEnergyDeliveredSum { get; init; }
    public required Energy RealEnergyConsumedSum { get; init; }
    public required Energy ReactiveEnergySum { get; init; }
}

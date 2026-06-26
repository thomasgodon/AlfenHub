using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Domain.Charging;

/// <summary>
/// Immutable snapshot of a socket's energy meter readings. Modelled as a value object: two
/// snapshots with the same readings are considered equal. Mirrors the Alfen "socket measurement"
/// register block (slave 1/2, registers 300-425): the per-phase values (L1/L2/L3) and the summed
/// totals.
/// </summary>
public sealed record EnergyMeasurements
{
    public required ushort MeterState { get; init; }
    public required MeterType MeterType { get; init; }
    /// <summary>Time elapsed since the meter last delivered a measurement.</summary>
    public required TimeSpan MeterLastValueTimestamp { get; init; }

    public required Voltage VoltageL1N { get; init; }
    public required Voltage VoltageL2N { get; init; }
    public required Voltage VoltageL3N { get; init; }
    public required Voltage VoltageL1L2 { get; init; }
    public required Voltage VoltageL2L3 { get; init; }
    public required Voltage VoltageL3L1 { get; init; }

    public required ElectricCurrent CurrentN { get; init; }
    public required ElectricCurrent CurrentL1 { get; init; }
    public required ElectricCurrent CurrentL2 { get; init; }
    public required ElectricCurrent CurrentL3 { get; init; }
    public required ElectricCurrent CurrentSum { get; init; }

    public required PowerFactor PowerFactorL1 { get; init; }
    public required PowerFactor PowerFactorL2 { get; init; }
    public required PowerFactor PowerFactorL3 { get; init; }
    public required PowerFactor PowerFactorSum { get; init; }

    public required Frequency Frequency { get; init; }

    public required Power RealPowerL1 { get; init; }
    public required Power RealPowerL2 { get; init; }
    public required Power RealPowerL3 { get; init; }
    public required Power RealPowerSum { get; init; }

    public required Power ApparentPowerL1 { get; init; }
    public required Power ApparentPowerL2 { get; init; }
    public required Power ApparentPowerL3 { get; init; }
    public required Power ApparentPowerSum { get; init; }

    public required Power ReactivePowerL1 { get; init; }
    public required Power ReactivePowerL2 { get; init; }
    public required Power ReactivePowerL3 { get; init; }
    public required Power ReactivePowerSum { get; init; }

    public required Energy RealEnergyDeliveredL1 { get; init; }
    public required Energy RealEnergyDeliveredL2 { get; init; }
    public required Energy RealEnergyDeliveredL3 { get; init; }
    public required Energy RealEnergyDeliveredSum { get; init; }

    public required Energy RealEnergyConsumedL1 { get; init; }
    public required Energy RealEnergyConsumedL2 { get; init; }
    public required Energy RealEnergyConsumedL3 { get; init; }
    public required Energy RealEnergyConsumedSum { get; init; }

    public required Energy ApparentEnergyL1 { get; init; }
    public required Energy ApparentEnergyL2 { get; init; }
    public required Energy ApparentEnergyL3 { get; init; }
    public required Energy ApparentEnergySum { get; init; }

    public required Energy ReactiveEnergyL1 { get; init; }
    public required Energy ReactiveEnergyL2 { get; init; }
    public required Energy ReactiveEnergyL3 { get; init; }
    public required Energy ReactiveEnergySum { get; init; }
}

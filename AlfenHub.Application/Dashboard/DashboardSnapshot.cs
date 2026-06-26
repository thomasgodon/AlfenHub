using AlfenHub.Domain.Charging;

namespace AlfenHub.Application.Dashboard;

/// <summary>
/// Flat, web-friendly projection of the <see cref="Charger"/> aggregate, pushed to the dashboard page
/// on every refresh. Units are normalised here (currents in A, voltages in V, power in W, frequency in
/// Hz, temperature in °C, energy in kWh) so the front-end can render values verbatim.
/// <para>
/// MAINTENANCE: when new data is read from the charger (new domain fields / Modbus registers), extend
/// this snapshot, the mapper below, and <c>wwwroot/index.html</c> in the same change.
/// </para>
/// </summary>
public record DashboardSnapshot
{
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Station-wide active maximum current setpoint, amperes.</summary>
    public required float StationActiveMaxCurrentA { get; init; }
    /// <summary>Station temperature, degrees Celsius.</summary>
    public required float TemperatureC { get; init; }
    public required ushort OcppState { get; init; }
    public required ushort TotalSockets { get; init; }

    /// <summary>Whether the KNX building-bus integration is enabled (shown as a connectivity pill).</summary>
    public required bool KnxEnabled { get; init; }

    public required IReadOnlyList<SocketDto> Sockets { get; init; }

    /// <summary>Projects a charger aggregate into a dashboard snapshot. Pure/synchronous so it is unit-testable.</summary>
    public static DashboardSnapshot FromCharger(Charger charger, bool knxEnabled, DateTimeOffset timestamp) => new()
    {
        Timestamp = timestamp,
        StationActiveMaxCurrentA = charger.StationActiveMaxCurrent.Amperes,
        TemperatureC = charger.Temperature.Celsius,
        OcppState = charger.OcppState,
        TotalSockets = charger.TotalSockets,
        KnxEnabled = knxEnabled,
        Sockets = charger.Sockets.Select(SocketDto.FromSocket).ToList()
    };
}

/// <summary>A single socket's full reading set. Voltages V, currents A, power W, frequency Hz, energy kWh.</summary>
public record SocketDto
{
    public required int Id { get; init; }

    // --- Energy measurements ---
    public required ushort MeterState { get; init; }
    public required float VoltageL1N { get; init; }
    public required float VoltageL2N { get; init; }
    public required float VoltageL3N { get; init; }
    public required float VoltageL1L2 { get; init; }
    public required float VoltageL2L3 { get; init; }
    public required float VoltageL3L1 { get; init; }
    public required float CurrentN { get; init; }
    public required float CurrentSum { get; init; }
    public required float PowerFactorSum { get; init; }
    public required float Frequency { get; init; }
    public required float RealPowerSum { get; init; }
    public required float ApparentPowerSum { get; init; }
    public required float ReactivePowerSum { get; init; }
    public required double RealEnergyDeliveredKwh { get; init; }
    public required double RealEnergyConsumedKwh { get; init; }
    public required double ReactiveEnergyKvarh { get; init; }

    // --- Charging status ---
    public required ushort Availability { get; init; }
    public required string Mode3State { get; init; }
    public required float ActualAppliedMaxCurrentA { get; init; }
    public required float SlaveMaxCurrentA { get; init; }
    public required double SlaveMaxCurrentValidForSeconds { get; init; }
    public required float ActiveLoadBalancingSafeCurrentA { get; init; }
    public required ushort ModbusSlaveReceivedSetpointAccountedFor { get; init; }
    public required ushort ChargePhases { get; init; }

    public static SocketDto FromSocket(Socket socket)
    {
        var energy = socket.EnergyMeasurements;
        var status = socket.Status;

        return new SocketDto
        {
            Id = socket.Id.Value,

            MeterState = energy.MeterState,
            VoltageL1N = energy.VoltageL1N.Volts,
            VoltageL2N = energy.VoltageL2N.Volts,
            VoltageL3N = energy.VoltageL3N.Volts,
            VoltageL1L2 = energy.VoltageL1L2.Volts,
            VoltageL2L3 = energy.VoltageL2L3.Volts,
            VoltageL3L1 = energy.VoltageL3L1.Volts,
            CurrentN = energy.CurrentN.Amperes,
            CurrentSum = energy.CurrentSum.Amperes,
            PowerFactorSum = energy.PowerFactorSum.Value,
            Frequency = energy.Frequency.Hertz,
            RealPowerSum = energy.RealPowerSum.Watts,
            ApparentPowerSum = energy.ApparentPowerSum.Watts,
            ReactivePowerSum = energy.ReactivePowerSum.Watts,
            RealEnergyDeliveredKwh = energy.RealEnergyDeliveredSum.WattHours / 1000.0,
            RealEnergyConsumedKwh = energy.RealEnergyConsumedSum.WattHours / 1000.0,
            ReactiveEnergyKvarh = energy.ReactiveEnergySum.WattHours / 1000.0,

            Availability = status.Availability,
            Mode3State = status.Mode3State.ToString(),
            ActualAppliedMaxCurrentA = status.ActualAppliedMaxCurrent.Amperes,
            SlaveMaxCurrentA = status.SlaveMaxCurrent.Current.Amperes,
            SlaveMaxCurrentValidForSeconds = status.SlaveMaxCurrent.ValidFor.TotalSeconds,
            ActiveLoadBalancingSafeCurrentA = status.ActiveLoadBalancingSafeCurrent.Amperes,
            ModbusSlaveReceivedSetpointAccountedFor = status.ModbusSlaveReceivedSetPointAccountedFor,
            ChargePhases = status.ChargePhases
        };
    }
}

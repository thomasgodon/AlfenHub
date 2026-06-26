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

    // --- Station status ---
    /// <summary>Station-wide active maximum current setpoint, amperes.</summary>
    public required float StationActiveMaxCurrentA { get; init; }
    /// <summary>Station temperature, degrees Celsius.</summary>
    public required float TemperatureC { get; init; }
    public required ushort OcppState { get; init; }
    public required ushort TotalSockets { get; init; }

    // --- Product identification ---
    public required string Name { get; init; }
    public required string Manufacturer { get; init; }
    public required short ModbusTableVersion { get; init; }
    public required string FirmwareVersion { get; init; }
    public required string PlatformType { get; init; }
    public required string SerialNumber { get; init; }
    /// <summary>Station wall-clock time; null when the station did not report a valid clock.</summary>
    public required DateTimeOffset? StationTime { get; init; }
    public required double UptimeSeconds { get; init; }
    public required int TimeZoneOffsetMinutes { get; init; }

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
        Name = charger.Name,
        Manufacturer = charger.Manufacturer,
        ModbusTableVersion = charger.ModbusTableVersion,
        FirmwareVersion = charger.FirmwareVersion,
        PlatformType = charger.PlatformType,
        SerialNumber = charger.SerialNumber,
        StationTime = charger.StationTime == default ? null : charger.StationTime,
        UptimeSeconds = charger.Uptime.TotalSeconds,
        TimeZoneOffsetMinutes = (int)charger.TimeZoneOffset.TotalMinutes,
        KnxEnabled = knxEnabled,
        Sockets = charger.Sockets.Select(SocketDto.FromSocket).ToList()
    };
}

/// <summary>A single socket's full reading set. Voltages V, currents A, power W, frequency Hz, energy kWh.</summary>
public record SocketDto
{
    public required int Id { get; init; }

    // --- Meter ---
    public required ushort MeterState { get; init; }
    public required string MeterType { get; init; }
    public required double MeterLastValueSeconds { get; init; }

    // --- Voltages ---
    public required float VoltageL1N { get; init; }
    public required float VoltageL2N { get; init; }
    public required float VoltageL3N { get; init; }
    public required float VoltageL1L2 { get; init; }
    public required float VoltageL2L3 { get; init; }
    public required float VoltageL3L1 { get; init; }

    // --- Currents ---
    public required float CurrentN { get; init; }
    public required float CurrentL1 { get; init; }
    public required float CurrentL2 { get; init; }
    public required float CurrentL3 { get; init; }
    public required float CurrentSum { get; init; }

    // --- Power factor ---
    public required float PowerFactorL1 { get; init; }
    public required float PowerFactorL2 { get; init; }
    public required float PowerFactorL3 { get; init; }
    public required float PowerFactorSum { get; init; }

    public required float Frequency { get; init; }

    // --- Real / apparent / reactive power ---
    public required float RealPowerL1 { get; init; }
    public required float RealPowerL2 { get; init; }
    public required float RealPowerL3 { get; init; }
    public required float RealPowerSum { get; init; }
    public required float ApparentPowerL1 { get; init; }
    public required float ApparentPowerL2 { get; init; }
    public required float ApparentPowerL3 { get; init; }
    public required float ApparentPowerSum { get; init; }
    public required float ReactivePowerL1 { get; init; }
    public required float ReactivePowerL2 { get; init; }
    public required float ReactivePowerL3 { get; init; }
    public required float ReactivePowerSum { get; init; }

    // --- Energy (kWh / kVAh / kvarh) ---
    public required double RealEnergyDeliveredL1Kwh { get; init; }
    public required double RealEnergyDeliveredL2Kwh { get; init; }
    public required double RealEnergyDeliveredL3Kwh { get; init; }
    public required double RealEnergyDeliveredKwh { get; init; }
    public required double RealEnergyConsumedL1Kwh { get; init; }
    public required double RealEnergyConsumedL2Kwh { get; init; }
    public required double RealEnergyConsumedL3Kwh { get; init; }
    public required double RealEnergyConsumedKwh { get; init; }
    public required double ApparentEnergyL1Kvah { get; init; }
    public required double ApparentEnergyL2Kvah { get; init; }
    public required double ApparentEnergyL3Kvah { get; init; }
    public required double ApparentEnergyKvah { get; init; }
    public required double ReactiveEnergyL1Kvarh { get; init; }
    public required double ReactiveEnergyL2Kvarh { get; init; }
    public required double ReactiveEnergyL3Kvarh { get; init; }
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
            MeterType = energy.MeterType.ToString(),
            MeterLastValueSeconds = energy.MeterLastValueTimestamp.TotalSeconds,

            VoltageL1N = energy.VoltageL1N.Volts,
            VoltageL2N = energy.VoltageL2N.Volts,
            VoltageL3N = energy.VoltageL3N.Volts,
            VoltageL1L2 = energy.VoltageL1L2.Volts,
            VoltageL2L3 = energy.VoltageL2L3.Volts,
            VoltageL3L1 = energy.VoltageL3L1.Volts,

            CurrentN = energy.CurrentN.Amperes,
            CurrentL1 = energy.CurrentL1.Amperes,
            CurrentL2 = energy.CurrentL2.Amperes,
            CurrentL3 = energy.CurrentL3.Amperes,
            CurrentSum = energy.CurrentSum.Amperes,

            PowerFactorL1 = energy.PowerFactorL1.Value,
            PowerFactorL2 = energy.PowerFactorL2.Value,
            PowerFactorL3 = energy.PowerFactorL3.Value,
            PowerFactorSum = energy.PowerFactorSum.Value,

            Frequency = energy.Frequency.Hertz,

            RealPowerL1 = energy.RealPowerL1.Watts,
            RealPowerL2 = energy.RealPowerL2.Watts,
            RealPowerL3 = energy.RealPowerL3.Watts,
            RealPowerSum = energy.RealPowerSum.Watts,
            ApparentPowerL1 = energy.ApparentPowerL1.Watts,
            ApparentPowerL2 = energy.ApparentPowerL2.Watts,
            ApparentPowerL3 = energy.ApparentPowerL3.Watts,
            ApparentPowerSum = energy.ApparentPowerSum.Watts,
            ReactivePowerL1 = energy.ReactivePowerL1.Watts,
            ReactivePowerL2 = energy.ReactivePowerL2.Watts,
            ReactivePowerL3 = energy.ReactivePowerL3.Watts,
            ReactivePowerSum = energy.ReactivePowerSum.Watts,

            RealEnergyDeliveredL1Kwh = energy.RealEnergyDeliveredL1.WattHours / 1000.0,
            RealEnergyDeliveredL2Kwh = energy.RealEnergyDeliveredL2.WattHours / 1000.0,
            RealEnergyDeliveredL3Kwh = energy.RealEnergyDeliveredL3.WattHours / 1000.0,
            RealEnergyDeliveredKwh = energy.RealEnergyDeliveredSum.WattHours / 1000.0,
            RealEnergyConsumedL1Kwh = energy.RealEnergyConsumedL1.WattHours / 1000.0,
            RealEnergyConsumedL2Kwh = energy.RealEnergyConsumedL2.WattHours / 1000.0,
            RealEnergyConsumedL3Kwh = energy.RealEnergyConsumedL3.WattHours / 1000.0,
            RealEnergyConsumedKwh = energy.RealEnergyConsumedSum.WattHours / 1000.0,
            ApparentEnergyL1Kvah = energy.ApparentEnergyL1.WattHours / 1000.0,
            ApparentEnergyL2Kvah = energy.ApparentEnergyL2.WattHours / 1000.0,
            ApparentEnergyL3Kvah = energy.ApparentEnergyL3.WattHours / 1000.0,
            ApparentEnergyKvah = energy.ApparentEnergySum.WattHours / 1000.0,
            ReactiveEnergyL1Kvarh = energy.ReactiveEnergyL1.WattHours / 1000.0,
            ReactiveEnergyL2Kvarh = energy.ReactiveEnergyL2.WattHours / 1000.0,
            ReactiveEnergyL3Kvarh = energy.ReactiveEnergyL3.WattHours / 1000.0,
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

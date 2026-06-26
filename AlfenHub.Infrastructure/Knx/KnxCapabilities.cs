namespace AlfenHub.Infrastructure.Knx;

/// <summary>
/// Capability strings — the contract between charger readings and KNX group addresses (the keys
/// under <c>KnxOptions.ReadGroupAddresses</c> / <c>WriteGroupAddresses</c> in appsettings).
/// <para>
/// Station-level readings are keyed <c>Station.{name}</c>; per-socket readings <c>Socket{id}.{name}</c>.
/// The capability suffixes are listed in <see cref="StationNames"/> / <see cref="SocketNames"/> and
/// emitted by <see cref="KnxReadingEncoder"/>; the appsettings keys must match exactly.
/// </para>
/// </summary>
internal static class KnxCapabilities
{
    /// <summary>The single writable capability: Socket 1's max-current setpoint (inbound KNX write).</summary>
    public const string SlaveMaxCurrent = "Socket1.SlaveMaxCurrent";

    public static string ForStation(string name) => $"Station.{name}";

    public static string ForSocket(int socketId, string name) => $"Socket{socketId}.{name}";

    /// <summary>Suffixes for the station-level (slave 200) capabilities.</summary>
    public static class StationNames
    {
        public const string ModbusTableVersion = "ModbusTableVersion";
        public const string ActiveMaxCurrent = "ActiveMaxCurrent";
        public const string Temperature = "Temperature";
        public const string OcppState = "OcppState";
        public const string NrOfSockets = "NrOfSockets";
        public const string Uptime = "Uptime";
        public const string TimeZone = "TimeZone";
        public const string StationTime = "StationTime";

        public static readonly string[] All =
        [
            ModbusTableVersion, ActiveMaxCurrent, Temperature, OcppState,
            NrOfSockets, Uptime, TimeZone, StationTime
        ];
    }

    /// <summary>Suffixes for the per-socket (slave 1/2) capabilities.</summary>
    public static class SocketNames
    {
        public const string MeterState = "MeterState";
        public const string MeterType = "MeterType";
        public const string MeterLastValueTimestamp = "MeterLastValueTimestamp";

        public const string VoltageL1N = "VoltageL1N";
        public const string VoltageL2N = "VoltageL2N";
        public const string VoltageL3N = "VoltageL3N";
        public const string VoltageL1L2 = "VoltageL1L2";
        public const string VoltageL2L3 = "VoltageL2L3";
        public const string VoltageL3L1 = "VoltageL3L1";

        public const string CurrentN = "CurrentN";
        public const string CurrentL1 = "CurrentL1";
        public const string CurrentL2 = "CurrentL2";
        public const string CurrentL3 = "CurrentL3";
        public const string CurrentSum = "CurrentSum";

        public const string PowerFactorL1 = "PowerFactorL1";
        public const string PowerFactorL2 = "PowerFactorL2";
        public const string PowerFactorL3 = "PowerFactorL3";
        public const string PowerFactorSum = "PowerFactorSum";

        public const string Frequency = "Frequency";

        public const string RealPowerL1 = "RealPowerL1";
        public const string RealPowerL2 = "RealPowerL2";
        public const string RealPowerL3 = "RealPowerL3";
        public const string RealPowerSum = "RealPowerSum";

        public const string ApparentPowerL1 = "ApparentPowerL1";
        public const string ApparentPowerL2 = "ApparentPowerL2";
        public const string ApparentPowerL3 = "ApparentPowerL3";
        public const string ApparentPowerSum = "ApparentPowerSum";

        public const string ReactivePowerL1 = "ReactivePowerL1";
        public const string ReactivePowerL2 = "ReactivePowerL2";
        public const string ReactivePowerL3 = "ReactivePowerL3";
        public const string ReactivePowerSum = "ReactivePowerSum";

        public const string RealEnergyDeliveredL1 = "RealEnergyDeliveredL1";
        public const string RealEnergyDeliveredL2 = "RealEnergyDeliveredL2";
        public const string RealEnergyDeliveredL3 = "RealEnergyDeliveredL3";
        public const string RealEnergyDeliveredSum = "RealEnergyDeliveredSum";

        public const string RealEnergyConsumedL1 = "RealEnergyConsumedL1";
        public const string RealEnergyConsumedL2 = "RealEnergyConsumedL2";
        public const string RealEnergyConsumedL3 = "RealEnergyConsumedL3";
        public const string RealEnergyConsumedSum = "RealEnergyConsumedSum";

        public const string ApparentEnergyL1 = "ApparentEnergyL1";
        public const string ApparentEnergyL2 = "ApparentEnergyL2";
        public const string ApparentEnergyL3 = "ApparentEnergyL3";
        public const string ApparentEnergySum = "ApparentEnergySum";

        public const string ReactiveEnergyL1 = "ReactiveEnergyL1";
        public const string ReactiveEnergyL2 = "ReactiveEnergyL2";
        public const string ReactiveEnergyL3 = "ReactiveEnergyL3";
        public const string ReactiveEnergySum = "ReactiveEnergySum";

        public const string Availability = "Availability";
        public const string Mode3State = "Mode3State";
        public const string ActualAppliedMaxCurrent = "ActualAppliedMaxCurrent";
        public const string SlaveMaxCurrent = "SlaveMaxCurrent";
        public const string SlaveMaxCurrentValidTime = "SlaveMaxCurrentValidTime";
        public const string ActiveLoadBalancingSafeCurrent = "ActiveLoadBalancingSafeCurrent";
        public const string ModbusSlaveReceivedSetpointAccountedFor = "ModbusSlaveReceivedSetpointAccountedFor";
        public const string ChargePhases = "ChargePhases";

        public static readonly string[] All =
        [
            MeterState, MeterType, MeterLastValueTimestamp,
            VoltageL1N, VoltageL2N, VoltageL3N, VoltageL1L2, VoltageL2L3, VoltageL3L1,
            CurrentN, CurrentL1, CurrentL2, CurrentL3, CurrentSum,
            PowerFactorL1, PowerFactorL2, PowerFactorL3, PowerFactorSum,
            Frequency,
            RealPowerL1, RealPowerL2, RealPowerL3, RealPowerSum,
            ApparentPowerL1, ApparentPowerL2, ApparentPowerL3, ApparentPowerSum,
            ReactivePowerL1, ReactivePowerL2, ReactivePowerL3, ReactivePowerSum,
            RealEnergyDeliveredL1, RealEnergyDeliveredL2, RealEnergyDeliveredL3, RealEnergyDeliveredSum,
            RealEnergyConsumedL1, RealEnergyConsumedL2, RealEnergyConsumedL3, RealEnergyConsumedSum,
            ApparentEnergyL1, ApparentEnergyL2, ApparentEnergyL3, ApparentEnergySum,
            ReactiveEnergyL1, ReactiveEnergyL2, ReactiveEnergyL3, ReactiveEnergySum,
            Availability, Mode3State, ActualAppliedMaxCurrent, SlaveMaxCurrent,
            SlaveMaxCurrentValidTime, ActiveLoadBalancingSafeCurrent,
            ModbusSlaveReceivedSetpointAccountedFor, ChargePhases
        ];
    }
}

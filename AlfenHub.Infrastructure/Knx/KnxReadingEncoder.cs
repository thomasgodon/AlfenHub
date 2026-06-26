using AlfenHub.Domain.Charging;

namespace AlfenHub.Infrastructure.Knx;

using Names = KnxCapabilities.SocketNames;
using StationNames = KnxCapabilities.StationNames;

/// <summary>
/// Encodes a charger snapshot into capability/byte pairs for KNX. Values are produced as wire-order
/// (big-endian) DPT payloads via <see cref="KnxDpt"/>; the building bus sends them verbatim. Kept as
/// a separate pure function so the encoding can be unit-tested without a live KNX connection.
/// <para>
/// Analog quantities are DPT 14 (4-byte float); energies are DPT 14 in kWh (preserves decimals);
/// counts/enums are 1-byte (DPT 5); durations are DPT 13 seconds; the station clock is DPT 19.
/// Identity strings are intentionally not exposed to KNX (dashboard only).
/// </para>
/// </summary>
internal static class KnxReadingEncoder
{
    public static IEnumerable<(string Capability, byte[] Value)> Encode(Charger charger)
    {
        yield return (KnxCapabilities.ForStation(StationNames.ModbusTableVersion), KnxDpt.UInt16(charger.ModbusTableVersion));
        yield return (KnxCapabilities.ForStation(StationNames.ActiveMaxCurrent), KnxDpt.Float(charger.StationActiveMaxCurrent.Amperes));
        yield return (KnxCapabilities.ForStation(StationNames.Temperature), KnxDpt.Float(charger.Temperature.Celsius));
        yield return (KnxCapabilities.ForStation(StationNames.OcppState), KnxDpt.UInt16(charger.OcppState));
        yield return (KnxCapabilities.ForStation(StationNames.NrOfSockets), KnxDpt.Byte(charger.TotalSockets));
        yield return (KnxCapabilities.ForStation(StationNames.Uptime), KnxDpt.Int32((long)charger.Uptime.TotalSeconds));
        yield return (KnxCapabilities.ForStation(StationNames.TimeZone), KnxDpt.Int16((int)charger.TimeZoneOffset.TotalMinutes));
        if (charger.StationTime != default)
        {
            yield return (KnxCapabilities.ForStation(StationNames.StationTime), KnxDpt.DateTime(charger.StationTime));
        }

        foreach (var socket in charger.Sockets)
        {
            foreach (var reading in EncodeSocket(socket))
            {
                yield return reading;
            }
        }
    }

    private static IEnumerable<(string Capability, byte[] Value)> EncodeSocket(Socket socket)
    {
        var id = socket.Id.Value;
        var energy = socket.EnergyMeasurements;
        var status = socket.Status;

        (string, byte[]) For(string name, byte[] value) => (KnxCapabilities.ForSocket(id, name), value);

        // Energy values are converted Wh -> kWh so the float keeps useful precision.
        static byte[] Kwh(double wattHours) => KnxDpt.Float((float)(wattHours / 1000.0));

        yield return For(Names.MeterState, KnxDpt.Byte(energy.MeterState));
        yield return For(Names.MeterType, KnxDpt.Byte((int)energy.MeterType));
        yield return For(Names.MeterLastValueTimestamp, KnxDpt.Int32((long)energy.MeterLastValueTimestamp.TotalSeconds));

        yield return For(Names.VoltageL1N, KnxDpt.Float(energy.VoltageL1N.Volts));
        yield return For(Names.VoltageL2N, KnxDpt.Float(energy.VoltageL2N.Volts));
        yield return For(Names.VoltageL3N, KnxDpt.Float(energy.VoltageL3N.Volts));
        yield return For(Names.VoltageL1L2, KnxDpt.Float(energy.VoltageL1L2.Volts));
        yield return For(Names.VoltageL2L3, KnxDpt.Float(energy.VoltageL2L3.Volts));
        yield return For(Names.VoltageL3L1, KnxDpt.Float(energy.VoltageL3L1.Volts));

        yield return For(Names.CurrentN, KnxDpt.Float(energy.CurrentN.Amperes));
        yield return For(Names.CurrentL1, KnxDpt.Float(energy.CurrentL1.Amperes));
        yield return For(Names.CurrentL2, KnxDpt.Float(energy.CurrentL2.Amperes));
        yield return For(Names.CurrentL3, KnxDpt.Float(energy.CurrentL3.Amperes));
        yield return For(Names.CurrentSum, KnxDpt.Float(energy.CurrentSum.Amperes));

        yield return For(Names.PowerFactorL1, KnxDpt.Float(energy.PowerFactorL1.Value));
        yield return For(Names.PowerFactorL2, KnxDpt.Float(energy.PowerFactorL2.Value));
        yield return For(Names.PowerFactorL3, KnxDpt.Float(energy.PowerFactorL3.Value));
        yield return For(Names.PowerFactorSum, KnxDpt.Float(energy.PowerFactorSum.Value));

        yield return For(Names.Frequency, KnxDpt.Float(energy.Frequency.Hertz));

        yield return For(Names.RealPowerL1, KnxDpt.Float(energy.RealPowerL1.Watts));
        yield return For(Names.RealPowerL2, KnxDpt.Float(energy.RealPowerL2.Watts));
        yield return For(Names.RealPowerL3, KnxDpt.Float(energy.RealPowerL3.Watts));
        yield return For(Names.RealPowerSum, KnxDpt.Float(energy.RealPowerSum.Watts));

        yield return For(Names.ApparentPowerL1, KnxDpt.Float(energy.ApparentPowerL1.Watts));
        yield return For(Names.ApparentPowerL2, KnxDpt.Float(energy.ApparentPowerL2.Watts));
        yield return For(Names.ApparentPowerL3, KnxDpt.Float(energy.ApparentPowerL3.Watts));
        yield return For(Names.ApparentPowerSum, KnxDpt.Float(energy.ApparentPowerSum.Watts));

        yield return For(Names.ReactivePowerL1, KnxDpt.Float(energy.ReactivePowerL1.Watts));
        yield return For(Names.ReactivePowerL2, KnxDpt.Float(energy.ReactivePowerL2.Watts));
        yield return For(Names.ReactivePowerL3, KnxDpt.Float(energy.ReactivePowerL3.Watts));
        yield return For(Names.ReactivePowerSum, KnxDpt.Float(energy.ReactivePowerSum.Watts));

        yield return For(Names.RealEnergyDeliveredL1, Kwh(energy.RealEnergyDeliveredL1.WattHours));
        yield return For(Names.RealEnergyDeliveredL2, Kwh(energy.RealEnergyDeliveredL2.WattHours));
        yield return For(Names.RealEnergyDeliveredL3, Kwh(energy.RealEnergyDeliveredL3.WattHours));
        yield return For(Names.RealEnergyDeliveredSum, Kwh(energy.RealEnergyDeliveredSum.WattHours));

        yield return For(Names.RealEnergyConsumedL1, Kwh(energy.RealEnergyConsumedL1.WattHours));
        yield return For(Names.RealEnergyConsumedL2, Kwh(energy.RealEnergyConsumedL2.WattHours));
        yield return For(Names.RealEnergyConsumedL3, Kwh(energy.RealEnergyConsumedL3.WattHours));
        yield return For(Names.RealEnergyConsumedSum, Kwh(energy.RealEnergyConsumedSum.WattHours));

        yield return For(Names.ApparentEnergyL1, Kwh(energy.ApparentEnergyL1.WattHours));
        yield return For(Names.ApparentEnergyL2, Kwh(energy.ApparentEnergyL2.WattHours));
        yield return For(Names.ApparentEnergyL3, Kwh(energy.ApparentEnergyL3.WattHours));
        yield return For(Names.ApparentEnergySum, Kwh(energy.ApparentEnergySum.WattHours));

        yield return For(Names.ReactiveEnergyL1, Kwh(energy.ReactiveEnergyL1.WattHours));
        yield return For(Names.ReactiveEnergyL2, Kwh(energy.ReactiveEnergyL2.WattHours));
        yield return For(Names.ReactiveEnergyL3, Kwh(energy.ReactiveEnergyL3.WattHours));
        yield return For(Names.ReactiveEnergySum, Kwh(energy.ReactiveEnergySum.WattHours));

        yield return For(Names.Availability, KnxDpt.Byte(status.Availability));
        yield return For(Names.Mode3State, KnxDpt.Byte((int)status.Mode3State));
        yield return For(Names.ActualAppliedMaxCurrent, KnxDpt.Float(status.ActualAppliedMaxCurrent.Amperes));
        yield return For(Names.SlaveMaxCurrent, KnxDpt.Float(status.SlaveMaxCurrent.Current.Amperes));
        yield return For(Names.SlaveMaxCurrentValidTime, KnxDpt.Int32((long)status.SlaveMaxCurrent.ValidFor.TotalSeconds));
        yield return For(Names.ActiveLoadBalancingSafeCurrent, KnxDpt.Float(status.ActiveLoadBalancingSafeCurrent.Amperes));
        yield return For(Names.ModbusSlaveReceivedSetpointAccountedFor, KnxDpt.Bool(status.ModbusSlaveReceivedSetPointAccountedFor != 0));
        yield return For(Names.ChargePhases, KnxDpt.Byte(status.ChargePhases));
    }
}

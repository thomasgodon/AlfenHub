using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Infrastructure.Knx;

/// <summary>
/// Encodes a charger snapshot into capability/byte pairs for KNX. Values are produced in app-side
/// byte order; the building bus reverses them before putting them on the wire. Kept as a separate
/// pure function so the encoding can be unit-tested without a live KNX connection.
/// </summary>
internal static class KnxReadingEncoder
{
    public static IEnumerable<(string Capability, byte[] Value)> Encode(Charger charger)
    {
        var socket = charger.FindSocket(new SocketId(1));
        if (socket is null)
        {
            yield break;
        }

        var energy = socket.EnergyMeasurements;
        var status = socket.Status;

        yield return (KnxCapabilities.MeterState, [(byte)Convert.ToSByte((int)energy.MeterState)]);
        yield return (KnxCapabilities.CurrentSum, BitConverter.GetBytes(energy.CurrentSum.Amperes));
        yield return (KnxCapabilities.PowerFactorSum, BitConverter.GetBytes(energy.PowerFactorSum.Value));
        yield return (KnxCapabilities.Frequency, BitConverter.GetBytes(energy.Frequency.Hertz));
        yield return (KnxCapabilities.RealPowerSum, BitConverter.GetBytes(energy.RealPowerSum.Watts));
        yield return (KnxCapabilities.ApparentPowerSum, BitConverter.GetBytes(energy.ApparentPowerSum.Watts));
        yield return (KnxCapabilities.ReactivePowerSum, BitConverter.GetBytes(energy.ReactivePowerSum.Watts));
        yield return (KnxCapabilities.RealEnergyDeliveredSum, BitConverter.GetBytes((int)energy.RealEnergyDeliveredSum.WattHours));
        yield return (KnxCapabilities.RealEnergyConsumedSum, BitConverter.GetBytes((int)energy.RealEnergyConsumedSum.WattHours));
        yield return (KnxCapabilities.ReactiveEnergySum, BitConverter.GetBytes((int)energy.ReactiveEnergySum.WattHours));
        yield return (KnxCapabilities.Mode3State, [(byte)Convert.ToSByte((int)status.Mode3State)]);
        yield return (KnxCapabilities.SlaveMaxCurrent, BitConverter.GetBytes(status.SlaveMaxCurrent.Current.Amperes));
        yield return (KnxCapabilities.ActualAppliedMaxCurrent, BitConverter.GetBytes(status.ActualAppliedMaxCurrent.Amperes));
    }
}

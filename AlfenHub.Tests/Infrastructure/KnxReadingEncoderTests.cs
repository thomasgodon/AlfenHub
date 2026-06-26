using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;
using AlfenHub.Infrastructure.Knx;
using AlfenHub.Tests.TestData;
using Xunit;

namespace AlfenHub.Tests.Infrastructure;

public class KnxReadingEncoderTests
{
    // 8 station-level capabilities (incl. StationTime) + the full per-socket set.
    private const int StationCapabilityCount = 8;
    private static readonly int SocketCapabilityCount = KnxCapabilities.SocketNames.All.Length;

    private static byte[] BigEndianFloat(float value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    [Fact]
    public void Encode_ProducesStationAndAllSocketCapabilities()
    {
        var charger = ChargerFactory.CreateSingleSocket();

        var encoded = KnxReadingEncoder.Encode(charger).Select(reading => reading.Capability).ToList();

        Assert.Equal(StationCapabilityCount + SocketCapabilityCount, encoded.Count);
        Assert.Contains("Station.Temperature", encoded);
        Assert.Contains("Station.StationTime", encoded);
        Assert.Contains("Socket1.CurrentSum", encoded);
        Assert.Contains("Socket1.RealPowerL2", encoded);
        Assert.Contains("Socket1.SlaveMaxCurrent", encoded);
    }

    [Fact]
    public void Encode_ProducesSocket2CapabilitiesForDualSocketStation()
    {
        var charger = ChargerFactory.CreateDualSocket();

        var encoded = KnxReadingEncoder.Encode(charger).Select(reading => reading.Capability).ToList();

        Assert.Equal(StationCapabilityCount + (2 * SocketCapabilityCount), encoded.Count);
        Assert.Contains("Socket2.CurrentSum", encoded);
        Assert.Contains("Socket2.RealPowerSum", encoded);
    }

    [Fact]
    public void Encode_EncodesFloatReadingsAsBigEndianWireBytes()
    {
        var charger = ChargerFactory.CreateSingleSocket(currentSum: 16f);

        var values = KnxReadingEncoder.Encode(charger).ToDictionary(r => r.Capability, r => r.Value);

        Assert.Equal(BigEndianFloat(16f), values["Socket1.CurrentSum"]);
    }

    [Fact]
    public void Encode_EncodesMeterStateAsSingleByte()
    {
        var charger = ChargerFactory.CreateSingleSocket(meterState: 5);

        var values = KnxReadingEncoder.Encode(charger).ToDictionary(r => r.Capability, r => r.Value);

        Assert.Equal(new byte[] { 5 }, values["Socket1.MeterState"]);
    }

    [Fact]
    public void Encode_EncodesEnergyAsFloatKilowattHours()
    {
        var charger = ChargerFactory.CreateSingleSocket(realEnergyDelivered: 1234d);

        var values = KnxReadingEncoder.Encode(charger).ToDictionary(r => r.Capability, r => r.Value);

        Assert.Equal(BigEndianFloat(1.234f), values["Socket1.RealEnergyDeliveredSum"]);
    }

    [Fact]
    public void Encode_WithNoSockets_StillProducesStationCapabilities()
    {
        var charger = Charger.FromSnapshot(
            stationActiveMaxCurrent: new ElectricCurrent(32f),
            temperature: new Temperature(20f),
            ocppState: 0,
            totalSockets: 0,
            sockets: []);

        var encoded = KnxReadingEncoder.Encode(charger).Select(reading => reading.Capability).ToList();

        // No StationTime (default clock) and no socket capabilities.
        Assert.Equal(StationCapabilityCount - 1, encoded.Count);
        Assert.DoesNotContain(encoded, c => c.StartsWith("Socket"));
        Assert.Contains("Station.Temperature", encoded);
    }
}

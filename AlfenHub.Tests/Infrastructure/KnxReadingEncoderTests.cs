using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;
using AlfenHub.Infrastructure.Knx;
using AlfenHub.Tests.TestData;
using Xunit;

namespace AlfenHub.Tests.Infrastructure;

public class KnxReadingEncoderTests
{
    [Fact]
    public void Encode_ProducesAllSocket1Capabilities()
    {
        var charger = ChargerFactory.CreateSingleSocket();

        var encoded = KnxReadingEncoder.Encode(charger).Select(reading => reading.Capability).ToList();

        Assert.Equal(13, encoded.Count);
        Assert.Contains("Socket1.CurrentSum", encoded);
        Assert.Contains("Socket1.SlaveMaxCurrent", encoded);
    }

    [Fact]
    public void Encode_EncodesFloatReadingsAsLittleEndianBytes()
    {
        var charger = ChargerFactory.CreateSingleSocket(currentSum: 16f);

        var values = KnxReadingEncoder.Encode(charger).ToDictionary(r => r.Capability, r => r.Value);

        Assert.Equal(BitConverter.GetBytes(16f), values["Socket1.CurrentSum"]);
    }

    [Fact]
    public void Encode_EncodesMeterStateAsSingleByte()
    {
        var charger = ChargerFactory.CreateSingleSocket(meterState: 5);

        var values = KnxReadingEncoder.Encode(charger).ToDictionary(r => r.Capability, r => r.Value);

        Assert.Equal(new byte[] { 5 }, values["Socket1.MeterState"]);
    }

    [Fact]
    public void Encode_EncodesEnergyAsTruncatedInteger()
    {
        var charger = ChargerFactory.CreateSingleSocket(realEnergyDelivered: 1234.9d);

        var values = KnxReadingEncoder.Encode(charger).ToDictionary(r => r.Capability, r => r.Value);

        Assert.Equal(BitConverter.GetBytes(1234), values["Socket1.RealEnergyDeliveredSum"]);
    }

    [Fact]
    public void Encode_ReturnsNothingWhenSocketMissing()
    {
        var charger = Charger.FromSnapshot(
            stationActiveMaxCurrent: new ElectricCurrent(32f),
            temperature: new Temperature(20f),
            ocppState: 0,
            totalSockets: 0,
            sockets: []);

        Assert.Empty(KnxReadingEncoder.Encode(charger));
    }
}

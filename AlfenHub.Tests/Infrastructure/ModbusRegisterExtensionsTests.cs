using System.Text;
using AlfenHub.Domain.Charging;
using AlfenHub.Infrastructure.Alfen;
using Xunit;

namespace AlfenHub.Tests.Infrastructure;

public class ModbusRegisterExtensionsTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(16.5f)]
    [InlineData(230.123f)]
    [InlineData(-12.25f)]
    public void ToFloat_RoundTripsToUshortArray(float value)
    {
        var registers = value.ToUshortArray();

        Assert.Equal(value, registers.ToFloat());
    }

    [Fact]
    public void ToFloat_MapsNaNToZero()
    {
        // Word-swapped representation of float.NaN.
        var nan = float.NaN.ToUshortArray();
        Assert.Equal(0f, nan.ToFloat());
    }

    [Fact]
    public void ToUshort_ReturnsFirstRegister()
    {
        Assert.Equal((ushort)1234, new ushort[] { 1234, 5678 }.ToUshort());
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1234d)]
    [InlineData(987654.5d)]
    public void ToDouble_DecodesWordSwappedDouble(double value)
    {
        Assert.Equal(value, EncodeDouble(value).ToDouble());
    }

    [Theory]
    [InlineData("A", Mode3State.A)]
    [InlineData("1B", Mode3State.B1)]
    [InlineData("2C", Mode3State.C2)]
    [InlineData("E", Mode3State.E)]
    [InlineData("??", Mode3State.F)]
    public void ToMode3State_DecodesStateString(string state, Mode3State expected)
    {
        Assert.Equal(expected, EncodeMode3State(state).ToMode3State());
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(60u)]
    [InlineData(3600u)]
    public void ToTimespan_DecodesSeconds(uint seconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(seconds), EncodeSeconds(seconds).ToTimespan());
    }

    [Fact]
    public void GetSection_ExtractsFloatAcrossBothRegisters()
    {
        // Regression guard: GetSection must copy the full register width, not half.
        var block = new ushort[126]; // covers absolute addresses 300..425
        var words = 230.5f.ToUshortArray();
        block[6] = words[0]; // register 306
        block[7] = words[1]; // register 307

        var value = block.GetSection(306, 307, 300, 425).ToFloat();

        Assert.Equal(230.5f, value);
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)2026)]
    [InlineData((short)-120)]
    public void ToShort_DecodesSigned16(short value)
    {
        Assert.Equal(value, new[] { unchecked((ushort)value) }.ToShort());
    }

    [Fact]
    public void ToAsciiString_DecodesTwoCharsPerRegisterAndStripsTrailingZero()
    {
        var registers = new ushort[]
        {
            (ushort)(('A' << 8) | 'L'),
            (ushort)(('F' << 8) | '_'),
            0x0000
        };

        Assert.Equal("ALF_", registers.ToAsciiString());
    }

    [Fact]
    public void ToMilliseconds_DecodesWordSwappedUnsigned64()
    {
        const ulong milliseconds = 123456UL;
        var bytes = BitConverter.GetBytes(milliseconds);
        var registers = new ushort[]
        {
            BitConverter.ToUInt16(bytes, 6),
            BitConverter.ToUInt16(bytes, 4),
            BitConverter.ToUInt16(bytes, 2),
            BitConverter.ToUInt16(bytes, 0),
        };

        Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), registers.ToMilliseconds());
    }

    [Fact]
    public void ToMilliseconds_ReturnsZeroForNaNFill()
    {
        var registers = new ushort[] { 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF };

        Assert.Equal(TimeSpan.Zero, registers.ToMilliseconds());
    }

    private static ushort[] EncodeDouble(double value)
    {
        var bytes = BitConverter.GetBytes(value);
        return
        [
            BitConverter.ToUInt16(bytes, 6),
            BitConverter.ToUInt16(bytes, 4),
            BitConverter.ToUInt16(bytes, 2),
            BitConverter.ToUInt16(bytes, 0),
        ];
    }

    private static ushort[] EncodeMode3State(string state)
    {
        var bytes = new byte[10];
        var encoded = Encoding.UTF8.GetBytes(state);
        Array.Copy(encoded, bytes, encoded.Length);
        return
        [
            BitConverter.ToUInt16(bytes, 8),
            BitConverter.ToUInt16(bytes, 6),
            BitConverter.ToUInt16(bytes, 4),
            BitConverter.ToUInt16(bytes, 2),
            BitConverter.ToUInt16(bytes, 0),
        ];
    }

    private static ushort[] EncodeSeconds(uint seconds)
    {
        var bytes = BitConverter.GetBytes(seconds);
        return
        [
            BitConverter.ToUInt16(bytes, 2),
            BitConverter.ToUInt16(bytes, 0),
        ];
    }
}

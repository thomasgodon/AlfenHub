using AlfenHub.Domain.ValueObjects;
using Xunit;

namespace AlfenHub.Tests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void ElectricCurrent_StoresAmperes()
    {
        var current = new ElectricCurrent(16.5f);
        Assert.Equal(16.5f, current.Amperes);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ElectricCurrent_RejectsNonFinite(float value)
    {
        Assert.Throws<ArgumentException>(() => new ElectricCurrent(value));
    }

    [Fact]
    public void ElectricCurrent_HasValueEquality()
    {
        Assert.Equal(new ElectricCurrent(10f), new ElectricCurrent(10f));
        Assert.NotEqual(new ElectricCurrent(10f), new ElectricCurrent(11f));
    }

    [Fact]
    public void Power_AllowsNegativeValues()
    {
        var power = new Power(-1500f);
        Assert.Equal(-1500f, power.Watts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SocketId_RejectsValuesBelowOne(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SocketId(value));
    }

    [Fact]
    public void SocketId_AcceptsOne()
    {
        Assert.Equal(1, new SocketId(1).Value);
    }
}

using AlfenHub.Application.Dashboard;
using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;
using AlfenHub.Tests.TestData;
using Xunit;

namespace AlfenHub.Tests.Dashboard;

public class ChargerDashboardSnapshotTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 6, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FromCharger_MapsStationAndSocketReadings()
    {
        var charger = ChargerFactory.CreateSingleSocket(
            currentSum: 16f,
            realPower: 3680f,
            mode3State: Mode3State.C2,
            slaveMaxCurrent: 10f);

        var snapshot = DashboardSnapshot.FromCharger(charger, knxEnabled: true, Timestamp);

        Assert.Equal(Timestamp, snapshot.Timestamp);
        Assert.True(snapshot.KnxEnabled);
        Assert.Equal(32f, snapshot.StationActiveMaxCurrentA);
        Assert.Equal(21.5f, snapshot.TemperatureC);
        Assert.Equal((ushort)1, snapshot.TotalSockets);

        var socket = Assert.Single(snapshot.Sockets);
        Assert.Equal(1, socket.Id);
        Assert.Equal(16f, socket.CurrentSum);
        Assert.Equal(3680f, socket.RealPowerSum);
        Assert.Equal(230f, socket.VoltageL1N);
        Assert.Equal("C2", socket.Mode3State);
        Assert.Equal(10f, socket.SlaveMaxCurrentA);
        Assert.Equal(60d, socket.SlaveMaxCurrentValidForSeconds);
        Assert.Equal((ushort)3, socket.ChargePhases);
        Assert.Equal("Rtu", socket.MeterType);
        Assert.Equal(3680f / 3f, socket.RealPowerL1);
    }

    [Fact]
    public void FromCharger_MapsProductIdentification()
    {
        var charger = ChargerFactory.CreateSingleSocket();

        var snapshot = DashboardSnapshot.FromCharger(charger, knxEnabled: false, Timestamp);

        Assert.Equal("ALF_1000", snapshot.Name);
        Assert.Equal("Alfen NV", snapshot.Manufacturer);
        Assert.Equal((short)1, snapshot.ModbusTableVersion);
        Assert.Equal("NG910", snapshot.PlatformType);
        Assert.Equal("00000R000", snapshot.SerialNumber);
        Assert.Equal(ChargerFactory.StationTime, snapshot.StationTime);
        Assert.Equal(TimeSpan.FromHours(50).TotalSeconds, snapshot.UptimeSeconds);
        Assert.Equal(120, snapshot.TimeZoneOffsetMinutes);
    }

    [Fact]
    public void FromCharger_MapsBothSockets()
    {
        var charger = ChargerFactory.CreateDualSocket();

        var snapshot = DashboardSnapshot.FromCharger(charger, knxEnabled: false, Timestamp);

        Assert.Equal(2, snapshot.Sockets.Count);
        Assert.Equal(2, snapshot.Sockets[1].Id);
        Assert.Equal("B1", snapshot.Sockets[1].Mode3State);
    }

    [Fact]
    public void FromCharger_ConvertsEnergyWattHoursToKilowattHours()
    {
        var charger = ChargerFactory.CreateSingleSocket(realEnergyDelivered: 1234d);

        var snapshot = DashboardSnapshot.FromCharger(charger, knxEnabled: false, Timestamp);

        Assert.False(snapshot.KnxEnabled);
        Assert.Equal(1.234d, Assert.Single(snapshot.Sockets).RealEnergyDeliveredKwh);
    }

    [Fact]
    public void FromCharger_WithNoSockets_ProducesEmptySocketList()
    {
        var charger = Charger.FromSnapshot(
            stationActiveMaxCurrent: new ElectricCurrent(32f),
            temperature: new Temperature(20f),
            ocppState: 0,
            totalSockets: 0,
            sockets: []);

        var snapshot = DashboardSnapshot.FromCharger(charger, knxEnabled: false, Timestamp);

        Assert.Empty(snapshot.Sockets);
    }
}

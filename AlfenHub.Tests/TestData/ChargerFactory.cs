using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Tests.TestData;

/// <summary>Builds fully-populated <see cref="Charger"/> aggregates for tests.</summary>
internal static class ChargerFactory
{
    public static readonly DateTimeOffset StationTime = new(2026, 6, 26, 14, 30, 0, TimeSpan.FromHours(2));

    public static Charger CreateSingleSocket(
        ushort meterState = 5,
        float currentSum = 16f,
        float realPower = 3680f,
        double realEnergyDelivered = 1234d,
        Mode3State mode3State = Mode3State.C2,
        float slaveMaxCurrent = 10f,
        float actualAppliedMaxCurrent = 8f)
    {
        var socket = BuildSocket(
            new SocketId(1), meterState, currentSum, realPower, realEnergyDelivered,
            mode3State, slaveMaxCurrent, actualAppliedMaxCurrent);

        return Build(totalSockets: 1, sockets: [socket]);
    }

    public static Charger CreateDualSocket()
    {
        var socket1 = BuildSocket(new SocketId(1), 5, 16f, 3680f, 1234d, Mode3State.C2, 10f, 8f);
        var socket2 = BuildSocket(new SocketId(2), 5, 8f, 1840f, 567d, Mode3State.B1, 6f, 6f);

        return Build(totalSockets: 2, sockets: [socket1, socket2]);
    }

    private static Charger Build(ushort totalSockets, IEnumerable<Socket> sockets) =>
        Charger.FromSnapshot(
            stationActiveMaxCurrent: new ElectricCurrent(32f),
            temperature: new Temperature(21.5f),
            ocppState: 2,
            totalSockets: totalSockets,
            sockets: sockets,
            name: "ALF_1000",
            manufacturer: "Alfen NV",
            modbusTableVersion: 1,
            firmwareVersion: "6.1.0-4159",
            platformType: "NG910",
            serialNumber: "00000R000",
            stationTime: StationTime,
            uptime: TimeSpan.FromHours(50),
            timeZoneOffset: TimeSpan.FromHours(2));

    private static Socket BuildSocket(
        SocketId id,
        ushort meterState,
        float currentSum,
        float realPower,
        double realEnergyDelivered,
        Mode3State mode3State,
        float slaveMaxCurrent,
        float actualAppliedMaxCurrent)
    {
        var energy = new EnergyMeasurements
        {
            MeterState = meterState,
            MeterType = MeterType.Rtu,
            MeterLastValueTimestamp = TimeSpan.FromMilliseconds(250),

            VoltageL1N = new Voltage(230f),
            VoltageL2N = new Voltage(231f),
            VoltageL3N = new Voltage(229f),
            VoltageL1L2 = new Voltage(400f),
            VoltageL2L3 = new Voltage(401f),
            VoltageL3L1 = new Voltage(399f),

            CurrentN = new ElectricCurrent(0.2f),
            CurrentL1 = new ElectricCurrent(currentSum / 3f),
            CurrentL2 = new ElectricCurrent(currentSum / 3f),
            CurrentL3 = new ElectricCurrent(currentSum / 3f),
            CurrentSum = new ElectricCurrent(currentSum),

            PowerFactorL1 = new PowerFactor(0.98f),
            PowerFactorL2 = new PowerFactor(0.97f),
            PowerFactorL3 = new PowerFactor(0.99f),
            PowerFactorSum = new PowerFactor(0.98f),

            Frequency = new Frequency(50f),

            RealPowerL1 = new Power(realPower / 3f),
            RealPowerL2 = new Power(realPower / 3f),
            RealPowerL3 = new Power(realPower / 3f),
            RealPowerSum = new Power(realPower),
            ApparentPowerL1 = new Power(realPower / 3f + 20f),
            ApparentPowerL2 = new Power(realPower / 3f + 20f),
            ApparentPowerL3 = new Power(realPower / 3f + 20f),
            ApparentPowerSum = new Power(realPower + 50f),
            ReactivePowerL1 = new Power(7f),
            ReactivePowerL2 = new Power(7f),
            ReactivePowerL3 = new Power(6f),
            ReactivePowerSum = new Power(20f),

            RealEnergyDeliveredL1 = new Energy(realEnergyDelivered / 3d),
            RealEnergyDeliveredL2 = new Energy(realEnergyDelivered / 3d),
            RealEnergyDeliveredL3 = new Energy(realEnergyDelivered / 3d),
            RealEnergyDeliveredSum = new Energy(realEnergyDelivered),
            RealEnergyConsumedL1 = new Energy(0d),
            RealEnergyConsumedL2 = new Energy(0d),
            RealEnergyConsumedL3 = new Energy(0d),
            RealEnergyConsumedSum = new Energy(0d),
            ApparentEnergyL1 = new Energy(10d),
            ApparentEnergyL2 = new Energy(10d),
            ApparentEnergyL3 = new Energy(10d),
            ApparentEnergySum = new Energy(30d),
            ReactiveEnergyL1 = new Energy(4d),
            ReactiveEnergyL2 = new Energy(4d),
            ReactiveEnergyL3 = new Energy(4d),
            ReactiveEnergySum = new Energy(12d),
        };

        var status = new ChargingStatus
        {
            Availability = 1,
            Mode3State = mode3State,
            ActualAppliedMaxCurrent = new ElectricCurrent(actualAppliedMaxCurrent),
            SlaveMaxCurrent = new MaxCurrentSetpoint(new ElectricCurrent(slaveMaxCurrent), TimeSpan.FromSeconds(60)),
            ActiveLoadBalancingSafeCurrent = new ElectricCurrent(6f),
            ModbusSlaveReceivedSetPointAccountedFor = 1,
            ChargePhases = 3,
        };

        return new Socket(id, energy, status);
    }
}

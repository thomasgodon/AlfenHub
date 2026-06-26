using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Tests.TestData;

/// <summary>Builds a fully-populated single-socket <see cref="Charger"/> for tests.</summary>
internal static class ChargerFactory
{
    public static Charger CreateSingleSocket(
        ushort meterState = 5,
        float currentSum = 16f,
        float realPower = 3680f,
        double realEnergyDelivered = 1234d,
        Mode3State mode3State = Mode3State.C2,
        float slaveMaxCurrent = 10f,
        float actualAppliedMaxCurrent = 8f)
    {
        var energy = new EnergyMeasurements
        {
            MeterState = meterState,
            VoltageL1N = new Voltage(230f),
            VoltageL2N = new Voltage(231f),
            VoltageL3N = new Voltage(229f),
            VoltageL1L2 = new Voltage(400f),
            VoltageL2L3 = new Voltage(401f),
            VoltageL3L1 = new Voltage(399f),
            CurrentN = new ElectricCurrent(0.2f),
            CurrentSum = new ElectricCurrent(currentSum),
            PowerFactorSum = new PowerFactor(0.98f),
            Frequency = new Frequency(50f),
            RealPowerSum = new Power(realPower),
            ApparentPowerSum = new Power(realPower + 50f),
            ReactivePowerSum = new Power(20f),
            RealEnergyDeliveredSum = new Energy(realEnergyDelivered),
            RealEnergyConsumedSum = new Energy(0d),
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

        var socket = new Socket(new SocketId(1), energy, status);

        return Charger.FromSnapshot(
            stationActiveMaxCurrent: new ElectricCurrent(32f),
            temperature: new Temperature(21.5f),
            ocppState: 2,
            totalSockets: 1,
            sockets: [socket]);
    }
}

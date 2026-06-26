using System.Net;
using AlfenHub.Domain.Abstractions;
using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;
using FluentModbus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlfenHub.Infrastructure.Alfen;

/// <summary>
/// Modbus TCP adapter for the Alfen charger. Implements the domain's <see cref="IChargerGateway"/>
/// port: reads the holding-register blocks and maps them into the <see cref="Charger"/> aggregate,
/// and writes the max-current setpoint. Owns the TCP connection and (re)connects lazily.
/// <para>
/// Currently only Socket 1 is populated even though the station reports a socket count. Extending
/// to Socket 2 means reading the <see cref="AlfenModbusConstants.Socket2SlaveAddress"/> block and
/// adding a second <see cref="Socket"/> here.
/// </para>
/// </summary>
internal sealed class AlfenModbusGateway : IChargerGateway, IDisposable
{
    private readonly ModbusTcpClient _modbusClient = new();
    private readonly AlfenModbusOptions _options;
    private readonly ILogger<AlfenModbusGateway> _logger;
    private IPEndPoint? _endPoint;

    public AlfenModbusGateway(IOptions<AlfenModbusOptions> options, ILogger<AlfenModbusGateway> logger)
    {
        _options = options.Value;
        _logger = logger;
        _modbusClient.ReadTimeout = 1000;
    }

    public async Task<Charger> GetAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        var stationStatusRegisters = (await _modbusClient.ReadHoldingRegistersAsync<ushort>(
            unitIdentifier: AlfenModbusConstants.StationStatusSlaveAddress,
            startingAddress: AlfenModbusConstants.StationStatusStartAddress,
            count: 6,
            cancellationToken)).ToArray();

        var socket1EnergyMeasurementsRegisters = (await _modbusClient.ReadHoldingRegistersAsync<ushort>(
            unitIdentifier: AlfenModbusConstants.Socket1SlaveAddress,
            startingAddress: AlfenModbusConstants.SocketEnergyMeasurementsStartAddress,
            count: 125,
            cancellationToken)).ToArray();

        var socket1StatusAndTransactionRegisters = (await _modbusClient.ReadHoldingRegistersAsync<ushort>(
            unitIdentifier: AlfenModbusConstants.Socket1SlaveAddress,
            startingAddress: AlfenModbusConstants.SocketStatusAndTransactionStartAddress,
            count: 16,
            cancellationToken)).ToArray();

        return MapCharger(stationStatusRegisters, socket1EnergyMeasurementsRegisters, socket1StatusAndTransactionRegisters);
    }

    public async Task WriteMaxCurrentAsync(SocketId socketId, ElectricCurrent maxCurrent, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        await _modbusClient.WriteMultipleRegistersAsync(
            unitIdentifier: socketId.Value,
            startingAddress: AlfenModbusConstants.ModbusSlaveMaxCurrentRegister,
            dataset: maxCurrent.Amperes.ToUshortArray(),
            cancellationToken);
    }

    private static Charger MapCharger(
        ushort[] stationStatusRegisters,
        ushort[] energyRegisters,
        ushort[] statusRegisters)
    {
        const int stationStart = AlfenModbusConstants.StationStatusStartAddress;
        const int stationEnd = AlfenModbusConstants.StationStatusEndAddress;
        const int energyStart = AlfenModbusConstants.SocketEnergyMeasurementsStartAddress;
        const int energyEnd = AlfenModbusConstants.SocketEnergyMeasurementsEndAddress;
        const int statusStart = AlfenModbusConstants.SocketStatusAndTransactionStartAddress;
        const int statusEnd = AlfenModbusConstants.SocketStatusAndTransactionEndAddress;

        var energyMeasurements = new EnergyMeasurements
        {
            MeterState = energyRegisters.GetSection(300, 300, energyStart, energyEnd).ToUshort(),
            VoltageL1N = new Voltage(energyRegisters.GetSection(306, 307, energyStart, energyEnd).ToFloat()),
            VoltageL2N = new Voltage(energyRegisters.GetSection(308, 309, energyStart, energyEnd).ToFloat()),
            VoltageL3N = new Voltage(energyRegisters.GetSection(310, 311, energyStart, energyEnd).ToFloat()),
            VoltageL1L2 = new Voltage(energyRegisters.GetSection(312, 313, energyStart, energyEnd).ToFloat()),
            VoltageL2L3 = new Voltage(energyRegisters.GetSection(314, 315, energyStart, energyEnd).ToFloat()),
            VoltageL3L1 = new Voltage(energyRegisters.GetSection(316, 317, energyStart, energyEnd).ToFloat()),
            CurrentN = new ElectricCurrent(energyRegisters.GetSection(318, 319, energyStart, energyEnd).ToFloat()),
            CurrentSum = new ElectricCurrent(energyRegisters.GetSection(326, 327, energyStart, energyEnd).ToFloat()),
            PowerFactorSum = new PowerFactor(energyRegisters.GetSection(334, 335, energyStart, energyEnd).ToFloat()),
            Frequency = new Frequency(energyRegisters.GetSection(336, 337, energyStart, energyEnd).ToFloat()),
            RealPowerSum = new Power(energyRegisters.GetSection(344, 345, energyStart, energyEnd).ToFloat()),
            ApparentPowerSum = new Power(energyRegisters.GetSection(352, 353, energyStart, energyEnd).ToFloat()),
            ReactivePowerSum = new Power(energyRegisters.GetSection(360, 361, energyStart, energyEnd).ToFloat()),
            RealEnergyDeliveredSum = new Energy(energyRegisters.GetSection(374, 377, energyStart, energyEnd).ToDouble()),
            RealEnergyConsumedSum = new Energy(energyRegisters.GetSection(390, 393, energyStart, energyEnd).ToDouble()),
            ReactiveEnergySum = new Energy(energyRegisters.GetSection(422, 425, energyStart, energyEnd).ToDouble()),
        };

        var status = new ChargingStatus
        {
            Availability = statusRegisters.GetSection(1200, 1200, statusStart, statusEnd).ToUshort(),
            Mode3State = statusRegisters.GetSection(1201, 1205, statusStart, statusEnd).ToMode3State(),
            ActualAppliedMaxCurrent = new ElectricCurrent(statusRegisters.GetSection(1206, 1207, statusStart, statusEnd).ToFloat()),
            SlaveMaxCurrent = new MaxCurrentSetpoint(
                new ElectricCurrent(statusRegisters.GetSection(1208, 1209, statusStart, statusEnd).ToFloat()),
                statusRegisters.GetSection(1208, 1209, statusStart, statusEnd).ToTimespan()),
            ActiveLoadBalancingSafeCurrent = new ElectricCurrent(statusRegisters.GetSection(1212, 1213, statusStart, statusEnd).ToFloat()),
            ModbusSlaveReceivedSetPointAccountedFor = statusRegisters.GetSection(1214, 1214, statusStart, statusEnd).ToUshort(),
            ChargePhases = statusRegisters.GetSection(1215, 1215, statusStart, statusEnd).ToUshort(),
        };

        var socket1 = new Socket(new SocketId(1), energyMeasurements, status);

        return Charger.FromSnapshot(
            stationActiveMaxCurrent: new ElectricCurrent(stationStatusRegisters.GetSection(1100, 1101, stationStart, stationEnd).ToFloat()),
            temperature: new Temperature(stationStatusRegisters.GetSection(1102, 1103, stationStart, stationEnd).ToFloat()),
            ocppState: stationStatusRegisters.GetSection(1104, 1104, stationStart, stationEnd).ToUshort(),
            totalSockets: stationStatusRegisters.GetSection(1105, 1105, stationStart, stationEnd).ToUshort(),
            sockets: [socket1]);
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_modbusClient.IsConnected)
        {
            return;
        }

        _endPoint ??= await ResolveEndPointAsync(cancellationToken);

        try
        {
            _modbusClient.Connect(_endPoint, ModbusEndianness.BigEndian);

            if (_modbusClient.IsConnected)
            {
                _logger.LogInformation("Connected to {Host} at port: {Port}", _endPoint.Address, _endPoint.Port);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong when trying to connect to {Host} at port: {Port}", _endPoint.Address, _endPoint.Port);
            throw;
        }
    }

    private async Task<IPEndPoint> ResolveEndPointAsync(CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(_options.Host, out var parsedIp))
        {
            return new IPEndPoint(parsedIp, _options.Port);
        }

        var hostEntry = await Dns.GetHostEntryAsync(_options.Host, cancellationToken);

        if (hostEntry.AddressList.Length == 0)
        {
            throw new ArgumentOutOfRangeException($"Could not resolve ip for host '{_options.Host}'");
        }

        parsedIp = hostEntry.AddressList[0].MapToIPv4();
        return new IPEndPoint(parsedIp, _options.Port);
    }

    public void Dispose() => _modbusClient.Dispose();
}

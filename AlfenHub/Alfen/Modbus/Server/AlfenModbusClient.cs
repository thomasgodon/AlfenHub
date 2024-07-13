using System.Net;
using System.Text.Json;
using AlfenHub.Alfen.Modbus.Models;
using AlfenHub.Alfen.Models;
using AlfenHub.Alfen.Notifications;
using FluentModbus;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlfenHub.Alfen.Modbus.Server;

internal class AlfenModbusClient : IAlfenModbusClient
{
    private readonly ILogger<AlfenModbusClient> _logger;
    private readonly IPublisher _publisher;
    private readonly AlfenModbusOptions _alfenModbusOptions;
    private readonly ModbusTcpClient _modbusClient;

    public AlfenModbusClient(
        ILogger<AlfenModbusClient> logger,
        IOptions<AlfenModbusOptions> alfenModbusOptions,
        IPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
        _alfenModbusOptions = alfenModbusOptions.Value;
        _logger = logger;
        _modbusClient = new ModbusTcpClient();
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        AlfenData? lastReceivedData;
        var endPoint = await GetEndPointAsync(cancellationToken);
        _modbusClient.ReadTimeout = 1000;

        await Task.Run(async () =>
        {
            // Keep this task alive until it is cancelled
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                if (!_modbusClient.IsConnected)
                {
                    try
                    {
                        _modbusClient.Connect(endPoint, ModbusEndianness.BigEndian);

                        if (_modbusClient.IsConnected)
                        {
                            _logger.LogInformation("Connected to {Host} at port: {Port}", endPoint.Address, endPoint.Port);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Something went wrong when trying to connect to {Host} at port: {Port}", endPoint.Address, endPoint.Port);
                        continue;
                    }
                }

                try
                {
                    lastReceivedData = await GetAlfenModbusData(cancellationToken);
                    _logger.LogTrace("{Message}", JsonSerializer.Serialize(lastReceivedData));

                    // notify new alfen data has arrived
                    await _publisher.Publish(new AlfenDataArrivedNotification(lastReceivedData), cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{Message}", e.Message);
                }
            }
        }, cancellationToken);
    }

    private async Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(_alfenModbusOptions.Host, out var parsedIp))
        {
            return new IPEndPoint(parsedIp, _alfenModbusOptions.Port);
        }

        var hostEntry = await Dns.GetHostEntryAsync(_alfenModbusOptions.Host, cancellationToken);

        if (hostEntry.AddressList.Length == 0)
        {
            throw new ArgumentOutOfRangeException($"Could not resolve ip for host '{_alfenModbusOptions.Host}'");
        }

        parsedIp = hostEntry.AddressList[0].MapToIPv4();
        return new IPEndPoint(parsedIp, _alfenModbusOptions.Port);
    }

    private async Task<AlfenData> GetAlfenModbusData(CancellationToken cancellationToken)
    {
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

        var sockets = stationStatusRegisters.GetSection(1105, 1105, AlfenModbusConstants.StationStatusStartAddress, AlfenModbusConstants.StationStatusEndAddress).ToUshort();

        var data = new AlfenData
        {
            StationActiveMaxCurrent = stationStatusRegisters.GetSection(1100, 1101, AlfenModbusConstants.StationStatusStartAddress, AlfenModbusConstants.StationStatusEndAddress).ToFloat(),
            Temperature = stationStatusRegisters.GetSection(1102, 1103, AlfenModbusConstants.StationStatusStartAddress, AlfenModbusConstants.StationStatusEndAddress).ToFloat(),
            OcppState = stationStatusRegisters.GetSection(1104, 1104, AlfenModbusConstants.StationStatusStartAddress, AlfenModbusConstants.StationStatusEndAddress).ToUshort(),
            TotalSockets = sockets,
            Socket1 = new AlfenSocket
            {
                EnergyMeasurements = new AlfenEnergyMeasurements
                {
                    VoltageL1N = socket1EnergyMeasurementsRegisters.GetSection(306, 307, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    VoltageL2N = socket1EnergyMeasurementsRegisters.GetSection(308, 309, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    VoltageL3N = socket1EnergyMeasurementsRegisters.GetSection(310, 311, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    VoltageL1L2 = socket1EnergyMeasurementsRegisters.GetSection(312, 313, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    VoltageL2L3 = socket1EnergyMeasurementsRegisters.GetSection(314, 315, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    VoltageL3L1 = socket1EnergyMeasurementsRegisters.GetSection(316, 317, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    CurrentN = socket1EnergyMeasurementsRegisters.GetSection(318, 319, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    CurrentPhaseL1 = socket1EnergyMeasurementsRegisters.GetSection(320, 321, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    CurrentPhaseL2 = socket1EnergyMeasurementsRegisters.GetSection(322, 323, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    CurrentPhaseL3 = socket1EnergyMeasurementsRegisters.GetSection(324, 325, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    CurrentSum = socket1EnergyMeasurementsRegisters.GetSection(326, 327, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    PowerFactorPhaseL1 = socket1EnergyMeasurementsRegisters.GetSection(328, 329, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    PowerFactorPhaseL2 = socket1EnergyMeasurementsRegisters.GetSection(330, 331, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    PowerFactorPhaseL3 = socket1EnergyMeasurementsRegisters.GetSection(332, 333, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    Frequency = socket1EnergyMeasurementsRegisters.GetSection(336, 337, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    RealPowerPhaseL1 = socket1EnergyMeasurementsRegisters.GetSection(338, 339, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    RealPowerPhaseL2 = socket1EnergyMeasurementsRegisters.GetSection(340, 341, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    RealPowerPhaseL3 = socket1EnergyMeasurementsRegisters.GetSection(342, 343, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                    RealPowerSum = socket1EnergyMeasurementsRegisters.GetSection(344, 345, AlfenModbusConstants.SocketEnergyMeasurementsStartAddress, AlfenModbusConstants.SocketEnergyMeasurementsEndAddress).ToFloat(),
                },
                StatusAndTransaction = new AlfenStatusAndTransaction
                {
                    Availability = socket1StatusAndTransactionRegisters.GetSection(1200, 1200, AlfenModbusConstants.SocketStatusAndTransactionStartAddress, AlfenModbusConstants.SocketStatusAndTransactionEndAddress).ToUshort(),
                    ActualAppliedMaxCurrent = socket1StatusAndTransactionRegisters.GetSection(1206, 1207, AlfenModbusConstants.SocketStatusAndTransactionStartAddress, AlfenModbusConstants.SocketStatusAndTransactionEndAddress).ToFloat(),
                    ModbusSlaveMaxCurrent = socket1StatusAndTransactionRegisters.GetSection(1208, 1209, AlfenModbusConstants.SocketStatusAndTransactionStartAddress, AlfenModbusConstants.SocketStatusAndTransactionEndAddress).ToFloat(),
                    ActiveLoadBalancingSafeCurrent = socket1StatusAndTransactionRegisters.GetSection(1212, 1213, AlfenModbusConstants.SocketStatusAndTransactionStartAddress, AlfenModbusConstants.SocketStatusAndTransactionEndAddress).ToFloat(),
                    ModbusSlaveReceivedSetPointAccountedFor = socket1StatusAndTransactionRegisters.GetSection(1214, 1214, AlfenModbusConstants.SocketStatusAndTransactionStartAddress, AlfenModbusConstants.SocketStatusAndTransactionEndAddress).ToUshort(),
                    ChargePhases = socket1StatusAndTransactionRegisters.GetSection(1215, 1215, AlfenModbusConstants.SocketStatusAndTransactionStartAddress, AlfenModbusConstants.SocketStatusAndTransactionEndAddress).ToUshort()
                }
            }
        };

        return data;
    }
}

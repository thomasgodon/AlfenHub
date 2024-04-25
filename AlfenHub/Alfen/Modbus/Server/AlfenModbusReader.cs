using System.Net.Sockets;
using AlfenHub.Alfen.Models;

namespace AlfenHub.Alfen.Modbus.Server;

internal partial class AlfenModbusClient
{
    private async Task<AlfenData> GetAlfenModbusData(CancellationToken cancellationToken)
    {
        var productIdentifierRegisters = await _modbusClient.ReadHoldingRegistersAsync<ushort>(200, 100, 78, cancellationToken);
        var stationStatusRegisters = await _modbusClient.ReadHoldingRegistersAsync<ushort>(200, 1100, 5, cancellationToken);
        var socket1Registers = Array.Empty<ushort>();


        var productIdentifierRegistersArray = productIdentifierRegisters.ToArray();
        var stationStatusRegistersArray = stationStatusRegisters.ToArray();

        var sockets = stationStatusRegistersArray.GetSection(1105, 1).ToUshort();
        var data = new AlfenData
        {
            StationActiveMaxCurrent = await GetStationActiveMaxCurrentAsync(cancellationToken),
            Temperature = await GetTemperatureAsync(cancellationToken),
            TotalSockets = sockets,
            Socket1 = new AlfenSocket
            {
                Frequency = await GetSocketFrequencyAsync(Socket1, cancellationToken),
                ModbusSlaveMaxCurrent = await GetSocketModbusSlaveMaxCurrentAsync(Socket1, cancellationToken),
                RealPowerPhaseL1 = await GetSocketRealPowerPhaseL1Async(Socket1, cancellationToken),
                RealPowerPhaseL2 = await GetSocketRealPowerPhaseL2Async(Socket1, cancellationToken),
                RealPowerPhaseL3 = await GetSocketRealPowerPhaseL3Async(Socket1, cancellationToken),
            },
            Socket2 = new AlfenSocket
            {
                Frequency = sockets >= Socket2 ? await GetSocketFrequencyAsync(Socket2, cancellationToken) : 0,
                ModbusSlaveMaxCurrent = sockets >= Socket2 ? await GetSocketModbusSlaveMaxCurrentAsync(Socket2, cancellationToken) : 0,
                RealPowerPhaseL1 = sockets >= Socket2 ? await GetSocketRealPowerPhaseL1Async(Socket2, cancellationToken) : 0,
                RealPowerPhaseL2 = sockets >= Socket2 ? await GetSocketRealPowerPhaseL2Async(Socket2, cancellationToken) : 0,
                RealPowerPhaseL3 = sockets >= Socket2 ? await GetSocketRealPowerPhaseL3Async(Socket2, cancellationToken) : 0,
            }
        };

        return data;
    }
}

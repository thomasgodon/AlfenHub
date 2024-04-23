using System.Net.Sockets;
using AlfenHub.Alfen.Models;

namespace AlfenHub.Alfen.Modbus.Server;

internal partial class AlfenModbusClient
{
    // TODO: read all at once

    private const int StationIdentifier = 200;
    private const int Socket1 = 1;
    private const int Socket2 = 2;

    private async Task<AlfenData> GetAlfenModbusData(CancellationToken cancellationToken)
    {
        var sockets = await GetTotalSocketsAsync(cancellationToken);
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

    private async Task<float> GetStationActiveMaxCurrentAsync(CancellationToken cancellationToken)
    {
        const ushort startingAddress = 1100;
        const ushort count = 2;
        var data = await _modbusClient.ReadHoldingRegistersAsync<ushort>(StationIdentifier, startingAddress, count, cancellationToken);
        return ConvertToFloat(data);
    }

    private async Task<float> GetTemperatureAsync(CancellationToken cancellationToken)
    {
        const ushort startingAddress = 1102;
        const ushort count = 2;
        var data = await _modbusClient.ReadHoldingRegistersAsync<ushort>(StationIdentifier, startingAddress, count, cancellationToken);
        return ConvertToFloat(data);
    }

    private async Task<ushort> GetTotalSocketsAsync(CancellationToken cancellationToken)
    {
        const ushort startingAddress = 1105;
        const ushort count = 1;
        var data = await _modbusClient.ReadHoldingRegistersAsync<ushort>(StationIdentifier, startingAddress, count, cancellationToken);
        return data.ToArray()[0];
    }

    private async Task<float> GetSocketFrequencyAsync(ushort socket, CancellationToken cancellationToken)
    {
        const ushort startingAddress = 336;
        const ushort count = 2;
        var data = await _modbusClient.ReadHoldingRegistersAsync<ushort>(socket, startingAddress, count, cancellationToken);
        return ConvertToFloat(data);
    }

    private async Task<float> GetSocketRealPowerPhaseL1Async(ushort socket, CancellationToken cancellationToken)
    {
        const ushort startingAddress = 338;
        const ushort count = 2;
        var data = await _modbusClient.ReadHoldingRegistersAsync<ushort>(socket, startingAddress, count, cancellationToken);
        return ConvertToFloat(data);
    }

    private async Task<float> GetSocketRealPowerPhaseL2Async(ushort socket, CancellationToken cancellationToken)
    {
        const ushort startingAddress = 340;
        const ushort count = 2;
        var data = await _modbusClient.ReadHoldingRegistersAsync<ushort>(socket, startingAddress, count, cancellationToken);
        return ConvertToFloat(data);
    }

    private async Task<float> GetSocketRealPowerPhaseL3Async(ushort socket, CancellationToken cancellationToken)
    {
        const ushort startingAddress = 342;
        const ushort count = 2;
        var data = await _modbusClient.ReadHoldingRegistersAsync<ushort>(socket, startingAddress, count, cancellationToken);
        return ConvertToFloat(data);
    }

    private async Task<float> GetSocketModbusSlaveMaxCurrentAsync(ushort socket, CancellationToken cancellationToken)
    {
        const ushort startingAddress = 1210;
        const ushort count = 2;
        var data = await _modbusClient.ReadHoldingRegistersAsync<ushort>(socket, startingAddress, count, cancellationToken);
        return ConvertToFloat(data);
    }

    private static float ConvertToFloat(Memory<ushort> data)
    {
        var bytes = BitConverter.GetBytes(data.ToArray()[1])
            .Concat(BitConverter.GetBytes(data.ToArray()[0])).ToArray();
        var value = BitConverter.ToSingle(bytes);
        return value is float.NaN ? 0f : value;
    }
}

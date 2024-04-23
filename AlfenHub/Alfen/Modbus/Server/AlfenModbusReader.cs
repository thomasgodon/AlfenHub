using AlfenHub.Alfen.Models;

namespace AlfenHub.Alfen.Modbus.Server;

internal partial class AlfenModbusClient
{
    private const int StationIdentifier = 200;

    private async Task<AlfenData> GetAlfenModbusData(CancellationToken cancellationToken) =>
        new()
        {
            StationActiveMaxCurrent = await GetStationActiveMaxCurrentAsync(cancellationToken),
            Temperature = await GetTemperatureAsync(cancellationToken),
            TotalSockets = await GetTotalSocketsAsync(cancellationToken)
        };

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

    private static float ConvertToFloat(Memory<ushort> data)
    {
        var bytes = BitConverter.GetBytes(data.ToArray()[1])
            .Concat(BitConverter.GetBytes(data.ToArray()[0])).ToArray();
        return BitConverter.ToSingle(bytes);
    }
}

using AlfenHub.Alfen.Models;

namespace AlfenHub.Alfen.Modbus.Server;

internal partial class AlfenModbusClient
{
    private const byte UnitIdentifier = 0x00;

    private async Task<AlfenData> GetAlfenModbusData(CancellationToken cancellationToken) =>
        new()
        {
            StationActiveMaxCurrent = await GetStationActiveMaxCurrentAsync(cancellationToken)
        };

    private async Task<double> GetStationActiveMaxCurrentAsync(CancellationToken cancellationToken)
    {
        const ushort startingAddress = 1100;
        const ushort count = 2;
        var data = await _modbusClient.ReadInputRegistersAsync<ushort>(UnitIdentifier, startingAddress, count, cancellationToken);
        return Math.Round((data.ToArray()[1] << 16 | data.ToArray()[0] & 0xffff) * 0.1, 2);
    }
}

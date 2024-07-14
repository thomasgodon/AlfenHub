using AlfenHub.Alfen.Models;

namespace AlfenHub.Alfen.Modbus.Server;

internal interface IAlfenModbusClient
{
    IReadOnlyDictionary<int, AlfenSocketWritableData> SocketWritableData { get; }
    Task Start(CancellationToken cancellationToken);
}

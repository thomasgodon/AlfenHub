namespace AlfenHub.Alfen.Modbus.Server;

internal interface IAlfenModbusClient
{
    Task Start(CancellationToken cancellationToken);
}

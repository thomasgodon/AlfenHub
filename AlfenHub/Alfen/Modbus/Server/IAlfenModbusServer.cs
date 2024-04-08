namespace AlfenHub.Alfen.Modbus.Server;

internal interface IAlfenModbusServer
{
    Task Start(CancellationToken cancellationToken);
}

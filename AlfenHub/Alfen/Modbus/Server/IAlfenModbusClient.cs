namespace AlfenHub.Alfen.Modbus.Server;

internal interface IAlfenModbusClient
{
    Task Start(CancellationToken cancellationToken);
    Task SetSlaveMaxCurrentAsync(uint socket, float actualCurrent, CancellationToken cancellationToken);
}

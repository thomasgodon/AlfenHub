using AlfenHub.Alfen.Modbus.Server;
using Microsoft.Extensions.Hosting;

namespace AlfenHub;

internal class Worker : BackgroundService
{
    private readonly IAlfenModbusClient _alfenModbusServer;

    public Worker(IAlfenModbusClient alfenModbusServer)
    {
        _alfenModbusServer = alfenModbusServer;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _alfenModbusServer.Start(cancellationToken);
    }
}
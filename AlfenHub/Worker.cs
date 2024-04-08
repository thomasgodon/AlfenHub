using AlfenHub.Alfen.Modbus.Server;
using Microsoft.Extensions.Hosting;

namespace AlfenHub;

internal class Worker : BackgroundService
{
    private readonly IAlfenModbusServer _alfenModbusServer;

    public Worker(IAlfenModbusServer alfenModbusServer)
    {
        _alfenModbusServer = alfenModbusServer;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _alfenModbusServer.Start(cancellationToken);
    }
}
using FluentModbus;
using Microsoft.Extensions.Logging;

namespace AlfenHub.Alfen.Modbus.Server;

internal class AlfenModbusServer : IAlfenModbusServer
{
    private readonly ILogger<AlfenModbusServer> _logger;
    private readonly ModbusTcpServer _modbusServer;

    public AlfenModbusServer(ILogger<AlfenModbusServer> logger)
    {
        _logger = logger;
        _modbusServer = new ModbusTcpServer(_logger, isAsynchronous: true);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        _modbusServer.Start();
        _modbusServer.RegistersChanged += async (_, args) =>
        {
            await RegistersChanged(args, cancellationToken);
        };
        var serverLock = _modbusServer.Lock;

        _logger.LogInformation("Modbus started at port 502");

        while (cancellationToken.IsCancellationRequested is false)
        {
            // add mediatr stuff

            // update server register content only once per second
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        _modbusServer.Dispose();
        _logger.LogWarning("Modbus stopped");
    }

    private Task RegistersChanged(RegistersChangedEventArgs e, CancellationToken cancellationToken)
    {
        foreach (var register in e.Registers)
        {
            _logger.LogTrace("Register changed: {register}", register);
        }
        return Task.CompletedTask;
    }
}

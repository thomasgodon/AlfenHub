using System.Net;
using System.Text.Json;
using AlfenHub.Alfen.Modbus.Models;
using AlfenHub.Alfen.Models;
using AlfenHub.Alfen.Notifications;
using FluentModbus;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlfenHub.Alfen.Modbus.Server;

internal partial class AlfenModbusClient : IAlfenModbusClient
{
    private readonly ILogger<AlfenModbusClient> _logger;
    private readonly IPublisher _publisher;
    private readonly AlfenModbusOptions _alfenModbusOptions;
    private readonly ModbusTcpClient _modbusClient;
    private AlfenData? _lastReceivedData;

    public AlfenModbusClient(
        ILogger<AlfenModbusClient> logger,
        IOptions<AlfenModbusOptions> alfenModbusOptions,
        IPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
        _alfenModbusOptions = alfenModbusOptions.Value;
        _logger = logger;
        _modbusClient = new ModbusTcpClient();
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        var endPoint = await GetEndPointAsync(cancellationToken);
        _modbusClient.ReadTimeout = 1000;

        await Task.Run(async () =>
        {
            // Keep this task alive until it is cancelled
            while (cancellationToken.IsCancellationRequested is false)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                if (_modbusClient.IsConnected is false)
                {
                    try
                    {
                        _modbusClient.Connect(endPoint, ModbusEndianness.BigEndian);

                        if (_modbusClient.IsConnected)
                        {
                            _logger.LogInformation("Connected to {host} at port: {port}", endPoint.Address, endPoint.Port);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception)
                    {
                        _logger.LogError("Something went wrong when trying to connect to {host} at port: {port}", endPoint.Address, endPoint.Port);
                        continue;
                    }
                }

                try
                {
                    _lastReceivedData = await GetAlfenModbusData(cancellationToken);
                    _logger.LogTrace("{message}", JsonSerializer.Serialize(_lastReceivedData));

                    // notify new alfen data has arrived
                    await _publisher.Publish(new AlfenDataArrivedNotification(_lastReceivedData), cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{message}", e.Message);
                }
            }
        }, cancellationToken);
    }

    private async Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(_alfenModbusOptions.Host, out var parsedIp))
        {
            return new IPEndPoint(parsedIp, _alfenModbusOptions.Port);
        }

        var hostEntry = await Dns.GetHostEntryAsync(_alfenModbusOptions.Host, cancellationToken);

        if (hostEntry.AddressList.Length == 0)
        {
            throw new ArgumentOutOfRangeException($"Could not resolve ip for host '{_alfenModbusOptions.Host}'");
        }

        parsedIp = hostEntry.AddressList[0].MapToIPv4();
        return new IPEndPoint(parsedIp, _alfenModbusOptions.Port);
    }
}

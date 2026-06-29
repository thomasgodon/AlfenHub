using System.Net;
using AlfenHub.Domain.Abstractions;
using AlfenHub.Domain.Charging;
using AlfenHub.Domain.ValueObjects;
using FluentModbus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlfenHub.Infrastructure.Alfen;

/// <summary>
/// Modbus TCP adapter for the Alfen charger. Implements the domain's <see cref="IChargerGateway"/>
/// port: reads the holding-register blocks (product identification, station status and per-socket
/// measurement/status) and maps them into the <see cref="Charger"/> aggregate, and writes the
/// max-current setpoint. Owns the TCP connection and (re)connects lazily.
/// <para>
/// Socket 2 is read only when the station status reports two sockets; the read is guarded so a
/// single-socket station that rejects slave address 2 falls back to one socket.
/// </para>
/// </summary>
internal sealed class AlfenModbusGateway : IChargerGateway, IDisposable
{
    private readonly ModbusTcpClient _modbusClient = new();
    private readonly AlfenModbusOptions _options;
    private readonly ILogger<AlfenModbusGateway> _logger;
    private IPEndPoint? _endPoint;

    public AlfenModbusGateway(IOptions<AlfenModbusOptions> options, ILogger<AlfenModbusGateway> logger)
    {
        _options = options.Value;
        _logger = logger;
        _modbusClient.ReadTimeout = 1000;
    }

    public async Task<Charger> GetAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        var productIdentificationRegisters = await ReadRegistersAsync(
            AlfenModbusConstants.StationSlaveAddress,
            AlfenModbusConstants.ProductIdentificationStartAddress,
            AlfenModbusConstants.ProductIdentificationRegisterCount,
            cancellationToken);

        var stationStatusRegisters = await ReadRegistersAsync(
            AlfenModbusConstants.StationSlaveAddress,
            AlfenModbusConstants.StationStatusStartAddress,
            AlfenModbusConstants.StationStatusRegisterCount,
            cancellationToken);

        var sockets = new List<Socket>
        {
            await ReadSocketAsync(AlfenModbusConstants.Socket1SlaveAddress, new SocketId(1), cancellationToken)
        };

        var totalSockets = stationStatusRegisters
            .GetSection(1105, 1105, AlfenModbusConstants.StationStatusStartAddress, AlfenModbusConstants.StationStatusEndAddress)
            .ToUshort();

        if (totalSockets >= 2)
        {
            try
            {
                sockets.Add(await ReadSocketAsync(AlfenModbusConstants.Socket2SlaveAddress, new SocketId(2), cancellationToken));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Could not read socket 2 even though the station reports {TotalSockets} sockets", totalSockets);
            }
        }

        return MapCharger(productIdentificationRegisters, stationStatusRegisters, sockets);
    }

    private async Task<Socket> ReadSocketAsync(int slaveAddress, SocketId socketId, CancellationToken cancellationToken)
    {
        var energyRegisters = await ReadRegistersAsync(
            slaveAddress,
            AlfenModbusConstants.SocketEnergyMeasurementsStartAddress,
            AlfenModbusConstants.SocketEnergyMeasurementsRegisterCount,
            cancellationToken);

        var statusRegisters = await ReadRegistersAsync(
            slaveAddress,
            AlfenModbusConstants.SocketStatusAndTransactionStartAddress,
            AlfenModbusConstants.SocketStatusAndTransactionRegisterCount,
            cancellationToken);

        return MapSocket(energyRegisters, statusRegisters, socketId);
    }

    /// <summary>
    /// Reads a contiguous holding-register range, splitting it into requests of at most 125 registers
    /// (the Modbus function-3 limit) and concatenating the result into a single block.
    /// </summary>
    private async Task<ushort[]> ReadRegistersAsync(int slaveAddress, int startAddress, int count, CancellationToken cancellationToken)
    {
        const int maxRegistersPerRequest = 125;

        var result = new ushort[count];
        var offset = 0;
        while (offset < count)
        {
            var chunk = Math.Min(maxRegistersPerRequest, count - offset);
            var data = (await InvokeAsync(() => _modbusClient.ReadHoldingRegistersAsync<ushort>(
                unitIdentifier: slaveAddress,
                startingAddress: startAddress + offset,
                count: chunk,
                cancellationToken))).ToArray();

            Array.Copy(data, 0, result, offset, chunk);
            offset += chunk;
        }

        return result;
    }

    public async Task WriteMaxCurrentAsync(SocketId socketId, ElectricCurrent maxCurrent, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        await InvokeAsync(() => _modbusClient.WriteMultipleRegistersAsync(
            unitIdentifier: socketId.Value,
            startingAddress: AlfenModbusConstants.ModbusSlaveMaxCurrentRegister,
            dataset: maxCurrent.Amperes.ToUshortArray(),
            cancellationToken));
    }

    /// <summary>
    /// Runs a Modbus I/O operation and, on a transport-level failure, drops the connection so the
    /// next poll cycle reconnects from scratch. After a timeout (or a half-open socket / network
    /// blip) FluentModbus still reports <c>IsConnected == true</c>, so without this
    /// <see cref="EnsureConnectedAsync"/> would never reconnect and every subsequent transaction
    /// would keep timing out. <see cref="ModbusException"/> is deliberately not caught — it is a
    /// valid protocol response on a healthy connection, so disconnecting on it would only churn.
    /// The exception is rethrown so the polling loop logs it.
    /// </summary>
    private async Task<T> InvokeAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (Exception e) when (e is TimeoutException or IOException or System.Net.Sockets.SocketException)
        {
            Disconnect();
            throw;
        }
    }

    private async Task InvokeAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception e) when (e is TimeoutException or IOException or System.Net.Sockets.SocketException)
        {
            Disconnect();
            throw;
        }
    }

    private void Disconnect()
    {
        try
        {
            _modbusClient.Disconnect();
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Error while disconnecting the Modbus client");
        }
    }

    private static Charger MapCharger(
        ushort[] productIdentificationRegisters,
        ushort[] stationStatusRegisters,
        IEnumerable<Socket> sockets)
    {
        const int idStart = AlfenModbusConstants.ProductIdentificationStartAddress;
        const int idEnd = AlfenModbusConstants.ProductIdentificationEndAddress;
        const int stationStart = AlfenModbusConstants.StationStatusStartAddress;
        const int stationEnd = AlfenModbusConstants.StationStatusEndAddress;

        var uptime = productIdentificationRegisters.GetSection(174, 177, idStart, idEnd).ToMilliseconds();
        var timeZoneOffset = TimeSpan.FromMinutes(productIdentificationRegisters.GetSection(178, 178, idStart, idEnd).ToShort());

        return Charger.FromSnapshot(
            stationActiveMaxCurrent: new ElectricCurrent(stationStatusRegisters.GetSection(1100, 1101, stationStart, stationEnd).ToFloat()),
            temperature: new Temperature(stationStatusRegisters.GetSection(1102, 1103, stationStart, stationEnd).ToFloat()),
            ocppState: stationStatusRegisters.GetSection(1104, 1104, stationStart, stationEnd).ToUshort(),
            totalSockets: stationStatusRegisters.GetSection(1105, 1105, stationStart, stationEnd).ToUshort(),
            sockets: sockets,
            name: productIdentificationRegisters.GetSection(100, 116, idStart, idEnd).ToAsciiString(),
            manufacturer: productIdentificationRegisters.GetSection(117, 121, idStart, idEnd).ToAsciiString(),
            modbusTableVersion: productIdentificationRegisters.GetSection(122, 122, idStart, idEnd).ToShort(),
            firmwareVersion: productIdentificationRegisters.GetSection(123, 139, idStart, idEnd).ToAsciiString(),
            platformType: productIdentificationRegisters.GetSection(140, 156, idStart, idEnd).ToAsciiString(),
            serialNumber: productIdentificationRegisters.GetSection(157, 167, idStart, idEnd).ToAsciiString(),
            stationTime: ComposeStationTime(productIdentificationRegisters, timeZoneOffset),
            uptime: uptime,
            timeZoneOffset: timeZoneOffset);
    }

    private static DateTimeOffset ComposeStationTime(ushort[] productIdentificationRegisters, TimeSpan timeZoneOffset)
    {
        const int idStart = AlfenModbusConstants.ProductIdentificationStartAddress;
        const int idEnd = AlfenModbusConstants.ProductIdentificationEndAddress;

        try
        {
            var year = productIdentificationRegisters.GetSection(168, 168, idStart, idEnd).ToShort();
            var month = productIdentificationRegisters.GetSection(169, 169, idStart, idEnd).ToShort();
            var day = productIdentificationRegisters.GetSection(170, 170, idStart, idEnd).ToShort();
            var hour = productIdentificationRegisters.GetSection(171, 171, idStart, idEnd).ToShort();
            var minute = productIdentificationRegisters.GetSection(172, 172, idStart, idEnd).ToShort();
            var second = productIdentificationRegisters.GetSection(173, 173, idStart, idEnd).ToShort();

            return new DateTimeOffset(year, month, day, hour, minute, second, timeZoneOffset);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Registers were unavailable (NaN fill) or out of range — report no clock.
            return default;
        }
    }

    private static Socket MapSocket(ushort[] energyRegisters, ushort[] statusRegisters, SocketId socketId)
    {
        const int energyStart = AlfenModbusConstants.SocketEnergyMeasurementsStartAddress;
        const int energyEnd = AlfenModbusConstants.SocketEnergyMeasurementsEndAddress;
        const int statusStart = AlfenModbusConstants.SocketStatusAndTransactionStartAddress;
        const int statusEnd = AlfenModbusConstants.SocketStatusAndTransactionEndAddress;

        var energyMeasurements = new EnergyMeasurements
        {
            MeterState = energyRegisters.GetSection(300, 300, energyStart, energyEnd).ToUshort(),
            MeterLastValueTimestamp = energyRegisters.GetSection(301, 304, energyStart, energyEnd).ToMilliseconds(),
            MeterType = ToMeterType(energyRegisters.GetSection(305, 305, energyStart, energyEnd).ToUshort()),

            VoltageL1N = new Voltage(energyRegisters.GetSection(306, 307, energyStart, energyEnd).ToFloat()),
            VoltageL2N = new Voltage(energyRegisters.GetSection(308, 309, energyStart, energyEnd).ToFloat()),
            VoltageL3N = new Voltage(energyRegisters.GetSection(310, 311, energyStart, energyEnd).ToFloat()),
            VoltageL1L2 = new Voltage(energyRegisters.GetSection(312, 313, energyStart, energyEnd).ToFloat()),
            VoltageL2L3 = new Voltage(energyRegisters.GetSection(314, 315, energyStart, energyEnd).ToFloat()),
            VoltageL3L1 = new Voltage(energyRegisters.GetSection(316, 317, energyStart, energyEnd).ToFloat()),

            CurrentN = new ElectricCurrent(energyRegisters.GetSection(318, 319, energyStart, energyEnd).ToFloat()),
            CurrentL1 = new ElectricCurrent(energyRegisters.GetSection(320, 321, energyStart, energyEnd).ToFloat()),
            CurrentL2 = new ElectricCurrent(energyRegisters.GetSection(322, 323, energyStart, energyEnd).ToFloat()),
            CurrentL3 = new ElectricCurrent(energyRegisters.GetSection(324, 325, energyStart, energyEnd).ToFloat()),
            CurrentSum = new ElectricCurrent(energyRegisters.GetSection(326, 327, energyStart, energyEnd).ToFloat()),

            PowerFactorL1 = new PowerFactor(energyRegisters.GetSection(328, 329, energyStart, energyEnd).ToFloat()),
            PowerFactorL2 = new PowerFactor(energyRegisters.GetSection(330, 331, energyStart, energyEnd).ToFloat()),
            PowerFactorL3 = new PowerFactor(energyRegisters.GetSection(332, 333, energyStart, energyEnd).ToFloat()),
            PowerFactorSum = new PowerFactor(energyRegisters.GetSection(334, 335, energyStart, energyEnd).ToFloat()),

            Frequency = new Frequency(energyRegisters.GetSection(336, 337, energyStart, energyEnd).ToFloat()),

            RealPowerL1 = new Power(energyRegisters.GetSection(338, 339, energyStart, energyEnd).ToFloat()),
            RealPowerL2 = new Power(energyRegisters.GetSection(340, 341, energyStart, energyEnd).ToFloat()),
            RealPowerL3 = new Power(energyRegisters.GetSection(342, 343, energyStart, energyEnd).ToFloat()),
            RealPowerSum = new Power(energyRegisters.GetSection(344, 345, energyStart, energyEnd).ToFloat()),

            ApparentPowerL1 = new Power(energyRegisters.GetSection(346, 347, energyStart, energyEnd).ToFloat()),
            ApparentPowerL2 = new Power(energyRegisters.GetSection(348, 349, energyStart, energyEnd).ToFloat()),
            ApparentPowerL3 = new Power(energyRegisters.GetSection(350, 351, energyStart, energyEnd).ToFloat()),
            ApparentPowerSum = new Power(energyRegisters.GetSection(352, 353, energyStart, energyEnd).ToFloat()),

            ReactivePowerL1 = new Power(energyRegisters.GetSection(354, 355, energyStart, energyEnd).ToFloat()),
            ReactivePowerL2 = new Power(energyRegisters.GetSection(356, 357, energyStart, energyEnd).ToFloat()),
            ReactivePowerL3 = new Power(energyRegisters.GetSection(358, 359, energyStart, energyEnd).ToFloat()),
            ReactivePowerSum = new Power(energyRegisters.GetSection(360, 361, energyStart, energyEnd).ToFloat()),

            RealEnergyDeliveredL1 = new Energy(energyRegisters.GetSection(362, 365, energyStart, energyEnd).ToDouble()),
            RealEnergyDeliveredL2 = new Energy(energyRegisters.GetSection(366, 369, energyStart, energyEnd).ToDouble()),
            RealEnergyDeliveredL3 = new Energy(energyRegisters.GetSection(370, 373, energyStart, energyEnd).ToDouble()),
            RealEnergyDeliveredSum = new Energy(energyRegisters.GetSection(374, 377, energyStart, energyEnd).ToDouble()),

            RealEnergyConsumedL1 = new Energy(energyRegisters.GetSection(378, 381, energyStart, energyEnd).ToDouble()),
            RealEnergyConsumedL2 = new Energy(energyRegisters.GetSection(382, 385, energyStart, energyEnd).ToDouble()),
            RealEnergyConsumedL3 = new Energy(energyRegisters.GetSection(386, 389, energyStart, energyEnd).ToDouble()),
            RealEnergyConsumedSum = new Energy(energyRegisters.GetSection(390, 393, energyStart, energyEnd).ToDouble()),

            ApparentEnergyL1 = new Energy(energyRegisters.GetSection(394, 397, energyStart, energyEnd).ToDouble()),
            ApparentEnergyL2 = new Energy(energyRegisters.GetSection(398, 401, energyStart, energyEnd).ToDouble()),
            ApparentEnergyL3 = new Energy(energyRegisters.GetSection(402, 405, energyStart, energyEnd).ToDouble()),
            ApparentEnergySum = new Energy(energyRegisters.GetSection(406, 409, energyStart, energyEnd).ToDouble()),

            ReactiveEnergyL1 = new Energy(energyRegisters.GetSection(410, 413, energyStart, energyEnd).ToDouble()),
            ReactiveEnergyL2 = new Energy(energyRegisters.GetSection(414, 417, energyStart, energyEnd).ToDouble()),
            ReactiveEnergyL3 = new Energy(energyRegisters.GetSection(418, 421, energyStart, energyEnd).ToDouble()),
            ReactiveEnergySum = new Energy(energyRegisters.GetSection(422, 425, energyStart, energyEnd).ToDouble()),
        };

        var status = new ChargingStatus
        {
            Availability = statusRegisters.GetSection(1200, 1200, statusStart, statusEnd).ToUshort(),
            Mode3State = statusRegisters.GetSection(1201, 1205, statusStart, statusEnd).ToMode3State(),
            ActualAppliedMaxCurrent = new ElectricCurrent(statusRegisters.GetSection(1206, 1207, statusStart, statusEnd).ToFloat()),
            // 1208-1209 = remaining valid time (UNSIGNED32, s); 1210-1211 = the max-current setpoint (FLOAT32, A).
            SlaveMaxCurrent = new MaxCurrentSetpoint(
                new ElectricCurrent(statusRegisters.GetSection(1210, 1211, statusStart, statusEnd).ToFloat()),
                statusRegisters.GetSection(1208, 1209, statusStart, statusEnd).ToTimespan()),
            ActiveLoadBalancingSafeCurrent = new ElectricCurrent(statusRegisters.GetSection(1212, 1213, statusStart, statusEnd).ToFloat()),
            ModbusSlaveReceivedSetPointAccountedFor = statusRegisters.GetSection(1214, 1214, statusStart, statusEnd).ToUshort(),
            ChargePhases = statusRegisters.GetSection(1215, 1215, statusStart, statusEnd).ToUshort(),
        };

        return new Socket(socketId, energyMeasurements, status);
    }

    private static MeterType ToMeterType(ushort value) => value switch
    {
        0 => MeterType.Rtu,
        1 => MeterType.TcpIp,
        2 => MeterType.Udp,
        3 => MeterType.P1,
        _ => MeterType.Other
    };

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_modbusClient.IsConnected)
        {
            return;
        }

        _endPoint ??= await ResolveEndPointAsync(cancellationToken);

        try
        {
            _modbusClient.Connect(_endPoint, ModbusEndianness.BigEndian);

            if (_modbusClient.IsConnected)
            {
                _logger.LogInformation("Connected to {Host} at port: {Port}", _endPoint.Address, _endPoint.Port);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong when trying to connect to {Host} at port: {Port}", _endPoint.Address, _endPoint.Port);
            throw;
        }
    }

    private async Task<IPEndPoint> ResolveEndPointAsync(CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(_options.Host, out var parsedIp))
        {
            return new IPEndPoint(parsedIp, _options.Port);
        }

        var hostEntry = await Dns.GetHostEntryAsync(_options.Host, cancellationToken);

        if (hostEntry.AddressList.Length == 0)
        {
            throw new ArgumentOutOfRangeException($"Could not resolve ip for host '{_options.Host}'");
        }

        parsedIp = hostEntry.AddressList[0].MapToIPv4();
        return new IPEndPoint(parsedIp, _options.Port);
    }

    public void Dispose() => _modbusClient.Dispose();
}

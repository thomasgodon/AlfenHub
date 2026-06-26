using AlfenHub.Domain.Charging.Events;
using AlfenHub.Domain.Common;
using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Domain.Charging;

/// <summary>
/// Aggregate root for the charging station. Holds the most recently read station-level state
/// (product identification + status) and its sockets. Reconstructed from a device read each polling
/// cycle; building a snapshot raises a <see cref="ChargerStateRefreshed"/> domain event.
/// </summary>
public sealed class Charger : AggregateRoot
{
    private readonly List<Socket> _sockets;

    private Charger(
        ElectricCurrent stationActiveMaxCurrent,
        Temperature temperature,
        ushort ocppState,
        ushort totalSockets,
        IEnumerable<Socket> sockets,
        string name,
        string manufacturer,
        short modbusTableVersion,
        string firmwareVersion,
        string platformType,
        string serialNumber,
        DateTimeOffset stationTime,
        TimeSpan uptime,
        TimeSpan timeZoneOffset)
    {
        StationActiveMaxCurrent = stationActiveMaxCurrent;
        Temperature = temperature;
        OcppState = ocppState;
        TotalSockets = totalSockets;
        _sockets = sockets.ToList();
        Name = name;
        Manufacturer = manufacturer;
        ModbusTableVersion = modbusTableVersion;
        FirmwareVersion = firmwareVersion;
        PlatformType = platformType;
        SerialNumber = serialNumber;
        StationTime = stationTime;
        Uptime = uptime;
        TimeZoneOffset = timeZoneOffset;
    }

    // --- Station status ---
    public ElectricCurrent StationActiveMaxCurrent { get; }
    public Temperature Temperature { get; }
    public ushort OcppState { get; }
    public ushort TotalSockets { get; }
    public IReadOnlyCollection<Socket> Sockets => _sockets;

    // --- Product identification ---
    public string Name { get; }
    public string Manufacturer { get; }
    public short ModbusTableVersion { get; }
    public string FirmwareVersion { get; }
    public string PlatformType { get; }
    public string SerialNumber { get; }
    /// <summary>The station's local wall-clock time (composed from the date/time registers).</summary>
    public DateTimeOffset StationTime { get; }
    public TimeSpan Uptime { get; }
    /// <summary>The station's configured time-zone offset from UTC.</summary>
    public TimeSpan TimeZoneOffset { get; }

    /// <summary>
    /// Builds a charger aggregate from a freshly read device snapshot and raises
    /// <see cref="ChargerStateRefreshed"/>. The product-identification arguments are optional so
    /// tests can construct minimal chargers.
    /// </summary>
    public static Charger FromSnapshot(
        ElectricCurrent stationActiveMaxCurrent,
        Temperature temperature,
        ushort ocppState,
        ushort totalSockets,
        IEnumerable<Socket> sockets,
        string name = "",
        string manufacturer = "",
        short modbusTableVersion = 0,
        string firmwareVersion = "",
        string platformType = "",
        string serialNumber = "",
        DateTimeOffset stationTime = default,
        TimeSpan uptime = default,
        TimeSpan timeZoneOffset = default)
    {
        var charger = new Charger(
            stationActiveMaxCurrent, temperature, ocppState, totalSockets, sockets,
            name, manufacturer, modbusTableVersion, firmwareVersion, platformType, serialNumber,
            stationTime, uptime, timeZoneOffset);
        charger.Raise(new ChargerStateRefreshed(charger));
        return charger;
    }

    public Socket? FindSocket(SocketId id) => _sockets.FirstOrDefault(socket => socket.Id == id);
}

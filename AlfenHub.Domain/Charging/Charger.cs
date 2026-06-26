using AlfenHub.Domain.Charging.Events;
using AlfenHub.Domain.Common;
using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Domain.Charging;

/// <summary>
/// Aggregate root for the charging station. Holds the most recently read station-level state and
/// its sockets. Reconstructed from a device read each polling cycle; building a snapshot raises a
/// <see cref="ChargerStateRefreshed"/> domain event.
/// </summary>
public sealed class Charger : AggregateRoot
{
    private readonly List<Socket> _sockets;

    private Charger(
        ElectricCurrent stationActiveMaxCurrent,
        Temperature temperature,
        ushort ocppState,
        ushort totalSockets,
        IEnumerable<Socket> sockets)
    {
        StationActiveMaxCurrent = stationActiveMaxCurrent;
        Temperature = temperature;
        OcppState = ocppState;
        TotalSockets = totalSockets;
        _sockets = sockets.ToList();
    }

    public ElectricCurrent StationActiveMaxCurrent { get; }
    public Temperature Temperature { get; }
    public ushort OcppState { get; }
    public ushort TotalSockets { get; }
    public IReadOnlyCollection<Socket> Sockets => _sockets;

    /// <summary>
    /// Builds a charger aggregate from a freshly read device snapshot and raises
    /// <see cref="ChargerStateRefreshed"/>.
    /// </summary>
    public static Charger FromSnapshot(
        ElectricCurrent stationActiveMaxCurrent,
        Temperature temperature,
        ushort ocppState,
        ushort totalSockets,
        IEnumerable<Socket> sockets)
    {
        var charger = new Charger(stationActiveMaxCurrent, temperature, ocppState, totalSockets, sockets);
        charger.Raise(new ChargerStateRefreshed(charger));
        return charger;
    }

    public Socket? FindSocket(SocketId id) => _sockets.FirstOrDefault(socket => socket.Id == id);
}

using System.Collections.Concurrent;
using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Application.Control;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="IChargerControlBuffer"/>. Keeps the latest
/// setpoint per socket so the polling loop can re-assert it every cycle.
/// </summary>
internal sealed class ChargerControlBuffer : IChargerControlBuffer
{
    private readonly ConcurrentDictionary<SocketId, ElectricCurrent> _setpoints = new();

    public void SetMaxCurrent(SocketId socketId, ElectricCurrent maxCurrent)
        => _setpoints[socketId] = maxCurrent;

    public IReadOnlyCollection<SocketMaxCurrent> GetPending()
        => _setpoints.Select(setpoint => new SocketMaxCurrent(setpoint.Key, setpoint.Value)).ToArray();
}

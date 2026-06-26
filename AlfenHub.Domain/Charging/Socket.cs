using AlfenHub.Domain.ValueObjects;

namespace AlfenHub.Domain.Charging;

/// <summary>
/// A socket on the charger, identified within the <see cref="Charger"/> aggregate by its
/// <see cref="SocketId"/>. Carries the latest energy measurements and charging status.
/// </summary>
public sealed class Socket
{
    public Socket(SocketId id, EnergyMeasurements energyMeasurements, ChargingStatus status)
    {
        Id = id;
        EnergyMeasurements = energyMeasurements;
        Status = status;
    }

    public SocketId Id { get; }
    public EnergyMeasurements EnergyMeasurements { get; }
    public ChargingStatus Status { get; }
}

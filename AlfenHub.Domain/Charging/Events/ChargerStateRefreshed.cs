using AlfenHub.Domain.Common;

namespace AlfenHub.Domain.Charging.Events;

/// <summary>
/// Raised whenever a fresh snapshot of the charger has been read from the device.
/// </summary>
public sealed record ChargerStateRefreshed(Charger Charger) : IDomainEvent;

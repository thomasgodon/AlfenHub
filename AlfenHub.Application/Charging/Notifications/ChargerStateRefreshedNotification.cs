using AlfenHub.Domain.Charging;
using MediatR;

namespace AlfenHub.Application.Charging.Notifications;

/// <summary>
/// Application-level notification carrying a fresh charger snapshot. Bridges the domain event
/// <c>ChargerStateRefreshed</c> onto MediatR so handlers (e.g. the building-bus projection) can react.
/// </summary>
public sealed record ChargerStateRefreshedNotification(Charger Charger) : INotification;

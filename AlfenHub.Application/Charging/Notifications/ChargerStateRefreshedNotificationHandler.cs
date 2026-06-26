using AlfenHub.Application.Abstractions;
using MediatR;

namespace AlfenHub.Application.Charging.Notifications;

/// <summary>
/// Projects a refreshed charger snapshot onto the building bus. Replaces the old
/// <c>KnxAlfenDataNotificationHandler</c>; the <see cref="IBuildingBus.IsEnabled"/> gate preserves
/// the previous "only when KNX is enabled" behaviour.
/// </summary>
internal sealed class ChargerStateRefreshedNotificationHandler
    : INotificationHandler<ChargerStateRefreshedNotification>
{
    private readonly IBuildingBus _buildingBus;

    public ChargerStateRefreshedNotificationHandler(IBuildingBus buildingBus)
    {
        _buildingBus = buildingBus;
    }

    public async Task Handle(ChargerStateRefreshedNotification notification, CancellationToken cancellationToken)
    {
        if (!_buildingBus.IsEnabled)
        {
            return;
        }

        await _buildingBus.PublishAsync(notification.Charger, cancellationToken);
    }
}

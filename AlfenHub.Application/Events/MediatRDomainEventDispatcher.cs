using AlfenHub.Application.Charging.Notifications;
using AlfenHub.Domain.Charging.Events;
using AlfenHub.Domain.Common;
using MediatR;

namespace AlfenHub.Application.Events;

/// <summary>
/// Translates domain events into MediatR notifications and publishes them. This is the single seam
/// where the (messaging-free) domain meets the application's MediatR pipeline.
/// </summary>
internal sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public MediatRDomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            INotification? notification = domainEvent switch
            {
                ChargerStateRefreshed refreshed => new ChargerStateRefreshedNotification(refreshed.Charger),
                _ => null
            };

            if (notification is not null)
            {
                await _publisher.Publish(notification, cancellationToken);
            }
        }
    }
}

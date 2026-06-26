using AlfenHub.Domain.Common;

namespace AlfenHub.Application.Events;

/// <summary>
/// Dispatches domain events raised by aggregates to the rest of the application. Keeps the domain
/// layer free of any messaging dependency.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}

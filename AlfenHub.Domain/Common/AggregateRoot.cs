namespace AlfenHub.Domain.Common;

/// <summary>
/// Base class for aggregate roots. Collects domain events raised while handling behaviour so the
/// application layer can drain and dispatch them after the aggregate has done its work.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Returns the pending domain events and clears the internal buffer.</summary>
    public IReadOnlyCollection<IDomainEvent> ReleaseEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}

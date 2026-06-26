namespace AlfenHub.Domain.Common;

/// <summary>
/// Marker for something that happened in the domain and is worth telling the rest of the
/// application about. Domain events are plain records here; the application layer is responsible
/// for dispatching them (e.g. by republishing them onto a message bus).
/// </summary>
public interface IDomainEvent;

using AlfenHub.Domain.ValueObjects;
using MediatR;

namespace AlfenHub.Application.Charging.Commands;

/// <summary>
/// Requests that a socket's max charging current be set. Raised by the building-bus adapter on an
/// inbound write. Replaces the old path that wrote straight into the shared <c>SocketWritableData</c>.
/// </summary>
public sealed record SetSocketMaxCurrentCommand(SocketId SocketId, ElectricCurrent MaxCurrent) : IRequest;

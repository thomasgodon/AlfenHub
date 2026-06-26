using AlfenHub.Application.Control;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AlfenHub.Application.Charging.Commands;

/// <summary>
/// Stores the requested setpoint in the control buffer. The polling loop re-asserts it to the
/// charger on its next cycle.
/// </summary>
internal sealed class SetSocketMaxCurrentCommandHandler : IRequestHandler<SetSocketMaxCurrentCommand>
{
    private readonly IChargerControlBuffer _controlBuffer;
    private readonly ILogger<SetSocketMaxCurrentCommandHandler> _logger;

    public SetSocketMaxCurrentCommandHandler(
        IChargerControlBuffer controlBuffer,
        ILogger<SetSocketMaxCurrentCommandHandler> logger)
    {
        _controlBuffer = controlBuffer;
        _logger = logger;
    }

    public Task Handle(SetSocketMaxCurrentCommand request, CancellationToken cancellationToken)
    {
        if (request.MaxCurrent.Amperes < 0)
        {
            _logger.LogWarning(
                "Ignoring negative max-current setpoint {Amperes} A for socket {SocketId}",
                request.MaxCurrent.Amperes,
                request.SocketId);
            return Task.CompletedTask;
        }

        _controlBuffer.SetMaxCurrent(request.SocketId, request.MaxCurrent);
        return Task.CompletedTask;
    }
}

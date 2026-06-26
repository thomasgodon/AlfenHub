using AlfenHub.Application.Charging;
using Microsoft.Extensions.Hosting;

namespace AlfenHub;

internal sealed class Worker : BackgroundService
{
    private readonly ChargerPollingService _pollingService;

    public Worker(ChargerPollingService pollingService)
    {
        _pollingService = pollingService;
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return _pollingService.RunAsync(cancellationToken);
    }
}

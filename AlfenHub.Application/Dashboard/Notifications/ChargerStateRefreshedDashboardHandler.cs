using AlfenHub.Application.Abstractions;
using AlfenHub.Application.Charging.Notifications;
using AlfenHub.Application.Dashboard.Options;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AlfenHub.Application.Dashboard.Notifications;

/// <summary>
/// Projects each refreshed charger snapshot into a <see cref="DashboardSnapshot"/> and publishes it to
/// the dashboard SSE broadcaster. Lives in the application layer (unlike SolaxHub's infrastructure
/// equivalent) because the <c>Charger</c> aggregate already carries every reading and MediatR only
/// scans the application assembly.
/// </summary>
internal sealed class ChargerStateRefreshedDashboardHandler
    : INotificationHandler<ChargerStateRefreshedNotification>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChargerSnapshotBroadcaster _broadcaster;
    private readonly IBuildingBus _buildingBus;
    private readonly DashboardOptions _dashboardOptions;

    public ChargerStateRefreshedDashboardHandler(
        IChargerSnapshotBroadcaster broadcaster,
        IBuildingBus buildingBus,
        IOptions<DashboardOptions> dashboardOptions)
    {
        _broadcaster = broadcaster;
        _buildingBus = buildingBus;
        _dashboardOptions = dashboardOptions.Value;
    }

    public Task Handle(ChargerStateRefreshedNotification notification, CancellationToken cancellationToken)
    {
        if (_dashboardOptions.Enabled is false)
            return Task.CompletedTask;

        var snapshot = DashboardSnapshot.FromCharger(notification.Charger, _buildingBus.IsEnabled, DateTimeOffset.UtcNow);
        _broadcaster.Publish(JsonSerializer.Serialize(snapshot, JsonOptions));

        return Task.CompletedTask;
    }
}

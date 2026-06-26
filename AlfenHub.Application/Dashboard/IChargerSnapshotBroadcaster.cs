using System.Threading.Channels;

namespace AlfenHub.Application.Dashboard;

/// <summary>
/// In-memory pub/sub that fans the latest <see cref="DashboardSnapshot"/> JSON out to every connected
/// dashboard SSE client. Holds the most recent snapshot so a freshly connected browser isn't blank.
/// </summary>
public interface IChargerSnapshotBroadcaster
{
    /// <summary>The most recently published snapshot JSON, or null if nothing has been published yet.</summary>
    string? Latest { get; }

    /// <summary>Publishes a snapshot to all current subscribers and caches it as <see cref="Latest"/>.</summary>
    void Publish(string snapshotJson);

    /// <summary>Subscribes a new SSE client; dispose the returned subscription on disconnect.</summary>
    (ChannelReader<string> Reader, IDisposable Subscription) Subscribe();
}

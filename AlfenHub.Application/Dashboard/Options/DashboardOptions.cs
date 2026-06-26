namespace AlfenHub.Application.Dashboard.Options;

/// <summary>
/// Configures the read-only web dashboard. When <see cref="Enabled"/> is true the host binds Kestrel
/// to <see cref="Port"/> and serves the live charger page; when false the host runs as a plain worker
/// with no listening port. Overridable via env vars (e.g. <c>DashboardOptions__Port</c>).
/// </summary>
public class DashboardOptions
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 8080;
}

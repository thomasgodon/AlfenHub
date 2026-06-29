using AlfenHub;
using AlfenHub.Application.Charging;
using AlfenHub.Application.Dashboard.Options;
using AlfenHub.Application.Extensions;
using AlfenHub.Dashboard;
using AlfenHub.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHostedService<Worker>()
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// The poll interval shares the Modbus configuration section.
builder.Services.Configure<ChargerPollingOptions>(builder.Configuration.GetSection("AlfenModbusOptions"));
builder.Services.Configure<DashboardOptions>(builder.Configuration.GetSection(nameof(DashboardOptions)));

var dashboardOptions = builder.Configuration.GetSection(nameof(DashboardOptions)).Get<DashboardOptions>() ?? new DashboardOptions();

// Bind Kestrel to the dashboard port when enabled; otherwise bind no endpoints so the host behaves
// like the original worker service (no listening port).
//
// When enabled we configure Kestrel explicitly via ListenAnyIP so the app ignores the ambient
// HTTP_PORTS/URLS environment entirely (the aspnet base image sets ASPNETCORE_HTTP_PORTS=8080).
// This binds exactly DashboardOptions.Port and avoids the misleading "Overriding HTTP_PORTS"
// warning. When disabled we set an empty URLS so Kestrel binds nothing (without it, Kestrel would
// fall back to its default :5000).
if (dashboardOptions.Enabled)
{
    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(dashboardOptions.Port));
}
else
{
    builder.WebHost.UseUrls(string.Empty);
}

var app = builder.Build();

if (dashboardOptions.Enabled)
{
    app.Logger.LogInformation("Dashboard listening on http://*:{Port}", dashboardOptions.Port);
    app.MapDashboard();
}
else
{
    app.Logger.LogInformation("Dashboard disabled; no HTTP port bound.");
}

await app.RunAsync();

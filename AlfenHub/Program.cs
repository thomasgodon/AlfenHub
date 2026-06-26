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
builder.WebHost.UseUrls(dashboardOptions.Enabled ? $"http://*:{dashboardOptions.Port}" : string.Empty);

var app = builder.Build();

if (dashboardOptions.Enabled)
{
    app.MapDashboard();
}

await app.RunAsync();

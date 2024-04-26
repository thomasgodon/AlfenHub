using AlfenHub;
using AlfenHub.Alfen.Extensions;
using AlfenHub.Alfen.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddHostedService<Worker>()
    .AddMediatR(m => m.RegisterServicesFromAssembly(typeof(Program).Assembly))
    .AddAlfen(builder.Configuration)
    .Configure()
    .AddOpenTelemetry()

    .ConfigureResource(resourceBuilder => resourceBuilder.AddService(nameof(AlfenHub))
        .AddAttributes([
            new KeyValuePair<string, object>("Environment", builder.Environment.EnvironmentName),
            new KeyValuePair<string, object>("Application", nameof(AlfenHub))
        ]))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(DiagnosticsConfig.ActivitySource.Name)
            .AddOtlpExporter(exporterOptions => { exporterOptions.Endpoint = new Uri("http://localhost:4317"); });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter()
            .AddOtlpExporter();
    });

builder.Logging
    .AddOpenTelemetry(loggingOptions =>
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(nameof(AlfenHub)).AddAttributes([
                new KeyValuePair<string, object>("Environment", builder.Environment.EnvironmentName)
            ]);
        loggingOptions.SetResourceBuilder(resourceBuilder);

        loggingOptions
            .AddOtlpExporter(exporterOptions => { exporterOptions.Endpoint = new Uri("http://localhost:4317"); });
    });

var host = builder.Build();

await host.RunAsync();

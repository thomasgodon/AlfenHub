using AlfenHub;
using AlfenHub.Application.Charging;
using AlfenHub.Application.Extensions;
using AlfenHub.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        services
            .AddHostedService<Worker>()
            .AddApplication()
            .AddInfrastructure(configuration);

        // The poll interval shares the Modbus configuration section.
        services.Configure<ChargerPollingOptions>(configuration.GetSection("AlfenModbusOptions"));
    })
    .Build();

await host.RunAsync();

using AlfenHub;
using AlfenHub.Alfen.Extensions;
using AlfenHub.Knx.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        services
            .AddHostedService<Worker>()
            .AddAlfen(configuration)
            .AddKnx(configuration)
            .AddMediatR(m => m.RegisterServicesFromAssembly(typeof(Program).Assembly));
    })
    .Build();

await host.RunAsync();

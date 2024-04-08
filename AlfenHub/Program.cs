using AlfenHub;
using AlfenHub.Alfen.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        services.AddHostedService<Worker>();
        services.AddAlfen(configuration);
        services.AddMediatR(m => m.RegisterServicesFromAssembly(typeof(Program).Assembly));
    })
    .Build();

await host.RunAsync();

using AlfenHub.Alfen.Modbus.Models;
using AlfenHub.Alfen.Modbus.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlfenHub.Alfen.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAlfen(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddModbusServer(configuration);

            return serviceCollection;
        }

        private static void AddModbusServer(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<AlfenModbusOptions>(configuration.GetSection(nameof(AlfenModbusOptions)));
            serviceCollection.AddSingleton<IAlfenModbusClient, AlfenModbusClient>();
        }

        public static IServiceCollection AddOpenTelemetryOption(this IServiceCollection serviceCollection, IConfiguration configuration)
        {

        }
    }
}

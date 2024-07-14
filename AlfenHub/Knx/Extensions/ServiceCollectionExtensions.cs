using AlfenHub.Knx.Client;
using AlfenHub.Knx.Models;
using AlfenHub.Knx.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlfenHub.Knx.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddKnx(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<KnxOptions>(configuration.GetSection(nameof(KnxOptions)));
            serviceCollection.AddSingleton<IKnxClient, KnxClient>();
            serviceCollection.AddSingleton<IKnxValueBufferService, KnxValueBufferService>();
            return serviceCollection;
        }
    }
}

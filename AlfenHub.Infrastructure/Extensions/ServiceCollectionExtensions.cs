using AlfenHub.Application.Abstractions;
using AlfenHub.Domain.Abstractions;
using AlfenHub.Infrastructure.Alfen;
using AlfenHub.Infrastructure.Knx;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlfenHub.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the infrastructure adapters: the Alfen Modbus charger gateway and the KNX building
    /// bus, plus their options.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<AlfenModbusOptions>(configuration.GetSection(nameof(AlfenModbusOptions)));
        serviceCollection.AddSingleton<IChargerGateway, AlfenModbusGateway>();

        serviceCollection.Configure<KnxOptions>(configuration.GetSection(nameof(KnxOptions)));
        serviceCollection.AddSingleton<IBuildingBus, KnxBuildingBus>();

        return serviceCollection;
    }
}

using AlfenHub.Application.Charging;
using AlfenHub.Application.Control;
using AlfenHub.Application.Events;
using Microsoft.Extensions.DependencyInjection;

namespace AlfenHub.Application.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the application layer: MediatR (commands, queries, notifications discovered in this
    /// assembly), the domain-event dispatcher, the charger control buffer and the polling service.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));

        serviceCollection.AddSingleton<IChargerControlBuffer, ChargerControlBuffer>();
        serviceCollection.AddSingleton<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        serviceCollection.AddSingleton<ChargerPollingService>();

        return serviceCollection;
    }
}

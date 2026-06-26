using AlfenHub.Domain.Charging;

namespace AlfenHub.Application.Abstractions;

/// <summary>
/// Outbound port to the building bus (KNX). The application projects charger state onto the bus;
/// the adapter is responsible for diffing, encoding and transporting the values, and for handling
/// inbound bus traffic (reads/writes) by dispatching application commands.
/// </summary>
public interface IBuildingBus
{
    /// <summary>Whether the building-bus integration is configured/enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Projects the latest charger snapshot onto the bus (sending only changed values).</summary>
    Task PublishAsync(Charger charger, CancellationToken cancellationToken);
}

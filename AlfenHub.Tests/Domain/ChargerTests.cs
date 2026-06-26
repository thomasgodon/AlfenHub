using AlfenHub.Domain.Charging.Events;
using AlfenHub.Domain.ValueObjects;
using AlfenHub.Tests.TestData;
using Xunit;

namespace AlfenHub.Tests.Domain;

public class ChargerTests
{
    [Fact]
    public void FromSnapshot_RaisesChargerStateRefreshed()
    {
        var charger = ChargerFactory.CreateSingleSocket();

        var domainEvent = Assert.Single(charger.DomainEvents);
        var refreshed = Assert.IsType<ChargerStateRefreshed>(domainEvent);
        Assert.Same(charger, refreshed.Charger);
    }

    [Fact]
    public void ReleaseEvents_ReturnsAndClearsEvents()
    {
        var charger = ChargerFactory.CreateSingleSocket();

        var released = charger.ReleaseEvents();

        Assert.Single(released);
        Assert.Empty(charger.DomainEvents);
    }

    [Fact]
    public void FindSocket_ReturnsSocketById()
    {
        var charger = ChargerFactory.CreateSingleSocket();

        Assert.NotNull(charger.FindSocket(new SocketId(1)));
        Assert.Null(charger.FindSocket(new SocketId(2)));
    }
}

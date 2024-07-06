namespace AlfenHub.Alfen.Models;

internal class AlfenSocket
{
    public AlfenEnergyMeasurements EnergyMeasurements { get; init; } = default!;
    public AlfenStatusAndTransaction StatusAndTransaction { get; init; } = default!;
}
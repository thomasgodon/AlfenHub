namespace AlfenHub.Domain.ValueObjects;

/// <summary>An amount of energy, expressed in watt-hours.</summary>
public readonly record struct Energy
{
    public double WattHours { get; }

    public Energy(double wattHours)
    {
        if (!double.IsFinite(wattHours))
        {
            throw new ArgumentException("Energy must be a finite value.", nameof(wattHours));
        }

        WattHours = wattHours;
    }

    public static Energy FromWattHours(double wattHours) => new(wattHours);

    public override string ToString() => $"{WattHours} Wh";
}

namespace AlfenHub.Domain.ValueObjects;

/// <summary>Electrical power, expressed in watts. May be negative (e.g. when exporting).</summary>
public readonly record struct Power
{
    public float Watts { get; }

    public Power(float watts)
    {
        if (!float.IsFinite(watts))
        {
            throw new ArgumentException("Power must be a finite value.", nameof(watts));
        }

        Watts = watts;
    }

    public static Power FromWatts(float watts) => new(watts);

    public override string ToString() => $"{Watts} W";
}

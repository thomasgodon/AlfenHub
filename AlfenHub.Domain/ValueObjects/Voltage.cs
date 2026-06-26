namespace AlfenHub.Domain.ValueObjects;

/// <summary>An electrical potential, expressed in volts.</summary>
public readonly record struct Voltage
{
    public float Volts { get; }

    public Voltage(float volts)
    {
        if (!float.IsFinite(volts))
        {
            throw new ArgumentException("Voltage must be a finite value.", nameof(volts));
        }

        Volts = volts;
    }

    public static Voltage FromVolts(float volts) => new(volts);

    public override string ToString() => $"{Volts} V";
}

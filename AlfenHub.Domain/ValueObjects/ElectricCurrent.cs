namespace AlfenHub.Domain.ValueObjects;

/// <summary>An electrical current, expressed in amperes.</summary>
public readonly record struct ElectricCurrent
{
    public float Amperes { get; }

    public ElectricCurrent(float amperes)
    {
        if (!float.IsFinite(amperes))
        {
            throw new ArgumentException("Electric current must be a finite value.", nameof(amperes));
        }

        Amperes = amperes;
    }

    public static ElectricCurrent FromAmperes(float amperes) => new(amperes);

    public override string ToString() => $"{Amperes} A";
}

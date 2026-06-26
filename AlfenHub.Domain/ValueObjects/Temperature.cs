namespace AlfenHub.Domain.ValueObjects;

/// <summary>A temperature, expressed in degrees Celsius.</summary>
public readonly record struct Temperature
{
    public float Celsius { get; }

    public Temperature(float celsius)
    {
        if (!float.IsFinite(celsius))
        {
            throw new ArgumentException("Temperature must be a finite value.", nameof(celsius));
        }

        Celsius = celsius;
    }

    public static Temperature FromCelsius(float celsius) => new(celsius);

    public override string ToString() => $"{Celsius} °C";
}

namespace AlfenHub.Domain.ValueObjects;

/// <summary>A frequency, expressed in hertz.</summary>
public readonly record struct Frequency
{
    public float Hertz { get; }

    public Frequency(float hertz)
    {
        if (!float.IsFinite(hertz))
        {
            throw new ArgumentException("Frequency must be a finite value.", nameof(hertz));
        }

        Hertz = hertz;
    }

    public static Frequency FromHertz(float hertz) => new(hertz);

    public override string ToString() => $"{Hertz} Hz";
}

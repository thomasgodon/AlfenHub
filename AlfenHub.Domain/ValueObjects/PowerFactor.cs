namespace AlfenHub.Domain.ValueObjects;

/// <summary>A power factor (dimensionless ratio, may be negative).</summary>
public readonly record struct PowerFactor
{
    public float Value { get; }

    public PowerFactor(float value)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentException("Power factor must be a finite value.", nameof(value));
        }

        Value = value;
    }

    public override string ToString() => Value.ToString("0.000");
}

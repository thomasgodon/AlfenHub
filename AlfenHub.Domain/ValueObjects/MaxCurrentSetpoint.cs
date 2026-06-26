namespace AlfenHub.Domain.ValueObjects;

/// <summary>
/// A maximum-current setpoint together with how long it remains valid. Alfen chargers fall back to a
/// safe current once the validity time elapses, which is why the setpoint must be re-asserted
/// periodically.
/// </summary>
public readonly record struct MaxCurrentSetpoint(ElectricCurrent Current, TimeSpan ValidFor)
{
    public override string ToString() => $"{Current} for {ValidFor}";
}

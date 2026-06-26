namespace AlfenHub.Application.Charging;

/// <summary>
/// How often the charger is polled. Bound from the same configuration section as the Modbus
/// connection options (it shares the <c>PollInterval</c> key) so existing config keeps working.
/// </summary>
public sealed class ChargerPollingOptions
{
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);
}

namespace AlfenHub.Infrastructure.Alfen;

/// <summary>
/// Connection options for the Alfen Modbus TCP endpoint. Bound from the <c>AlfenModbusOptions</c>
/// configuration section. (The poll interval is bound separately into the application layer's
/// <c>ChargerPollingOptions</c> from the same section.)
/// </summary>
public sealed class AlfenModbusOptions
{
    public string Host { get; init; } = default!;
    public int Port { get; init; } = 502;
}

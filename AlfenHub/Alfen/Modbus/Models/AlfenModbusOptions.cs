namespace AlfenHub.Alfen.Modbus.Models
{
    public class AlfenModbusOptions
    {
        public string Host { get; init; } = default!;
        public int Port { get; init; } = default!;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);
    }
}

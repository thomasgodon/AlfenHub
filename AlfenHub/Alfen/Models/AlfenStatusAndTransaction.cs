namespace AlfenHub.Alfen.Models
{
    internal class AlfenStatusAndTransaction
    {
        public ushort Availability { get; init; } = default!;
        public float ActualAppliedMaxCurrent { get; init; } = default!;
        public float ModbusSlaveMaxCurrent { get; init; } = default!;
        public float ActiveLoadBalancingSafeCurrent { get; init; } = default!;
        public ushort ModbusSlaveReceivedSetPointAccountedFor { get; init; } = default!;
        public ushort ChargePhases { get; init; } = default!;
    }
}

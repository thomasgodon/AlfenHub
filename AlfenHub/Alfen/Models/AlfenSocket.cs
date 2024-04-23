namespace AlfenHub.Alfen.Models;

internal class AlfenSocket
{
    public float Frequency { get; init; } = default!;
    public float ModbusSlaveMaxCurrent { get; init; } = default!;
    public float RealPowerPhaseL1 { get; init; } = default!;
    public float RealPowerPhaseL2 { get; init; } = default!;
    public float RealPowerPhaseL3 { get; init; } = default!;
}
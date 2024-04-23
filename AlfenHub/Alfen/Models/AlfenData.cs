namespace AlfenHub.Alfen.Models;

internal class AlfenData
{
    public float StationActiveMaxCurrent { get; init; } = default!;
    public float Temperature { get; init; } = default!;
    public ushort TotalSockets { get; init; } = default!;
    public AlfenSocket Socket1 { get; init; } = default!;
    public AlfenSocket Socket2 { get; init; } = default!;
}

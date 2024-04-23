namespace AlfenHub.Alfen.Models;

internal class AlfenData
{
    public double StationActiveMaxCurrent { get; init; } = default!;
    public double Temperature { get; init; } = default!;
    public ushort TotalSockets { get; init; } = default!;
}

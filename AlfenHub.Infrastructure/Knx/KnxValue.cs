using Knx.Falcon;

namespace AlfenHub.Infrastructure.Knx;

/// <summary>
/// A KNX group address together with its last-known (decoded, app-side byte order) value. The byte
/// order is reversed when written to the bus.
/// </summary>
internal sealed class KnxValue
{
    public KnxValue(GroupAddress address)
    {
        Address = address;
    }

    public GroupAddress Address { get; }
    public byte[]? Value { get; internal set; }

    public override string ToString()
    {
        var value = Value is not null ? string.Join(",", Value.ToList()) : string.Empty;
        return $"{Address} - {value}";
    }
}

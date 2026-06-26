using Knx.Falcon;

namespace AlfenHub.Infrastructure.Knx;

/// <summary>
/// Options for the KNX building-bus adapter. Bound from the <c>KnxOptions</c> configuration section.
/// An empty group address means the capability is skipped.
/// </summary>
internal sealed class KnxOptions
{
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 3671;
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);
    public IndividualAddress IndividualAddress { get; set; } = default!;
    public Dictionary<string, string> ReadGroupAddresses { get; set; } = default!;
    public Dictionary<string, string> WriteGroupAddresses { get; set; } = default!;
}

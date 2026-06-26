namespace AlfenHub.Infrastructure.Knx;

internal static class KnxOptionsExtensions
{
    public static IEnumerable<KeyValuePair<string, string>> GetReadGroupAddressesFromOptions(this KnxOptions options)
        => options.ReadGroupAddresses.Where(mapping => !string.IsNullOrEmpty(mapping.Value));

    public static IEnumerable<KeyValuePair<string, string>> GetWriteGroupAddressesFromOptions(this KnxOptions options)
        => options.WriteGroupAddresses.Where(mapping => !string.IsNullOrEmpty(mapping.Value));
}

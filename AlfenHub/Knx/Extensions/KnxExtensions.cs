using AlfenHub.Knx.Models;

namespace AlfenHub.Knx.Extensions
{
    internal static class KnxExtensions
    {
        public static IEnumerable<KeyValuePair<string, string>> GetReadGroupAddressesFromOptions(this KnxOptions options)
            => options.ReadGroupAddresses
                .Where(
                    mapping => !string.IsNullOrEmpty(mapping.Value));

        public static IEnumerable<KeyValuePair<string, string>> GetWriteGroupAddressesFromOptions(this KnxOptions options)
            => options.WriteGroupAddresses
                .Where(
                    mapping => !string.IsNullOrEmpty(mapping.Value));
    }
}

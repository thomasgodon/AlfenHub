using AlfenHub.Knx.Models;

namespace AlfenHub.Knx.Client;

internal interface IKnxClient
{
    Task SendValuesAsync(IEnumerable<KnxValue> values, CancellationToken cancellationToken);
}
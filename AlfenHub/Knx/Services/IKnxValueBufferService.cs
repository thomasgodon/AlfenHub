using AlfenHub.Alfen.Models;
using AlfenHub.Knx.Models;

namespace AlfenHub.Knx.Services
{
    internal interface IKnxValueBufferService
    {
        IEnumerable<KnxValue> UpdateKnxValues(AlfenData data);
        IReadOnlyDictionary<string, KnxValue> GetKnxValues();
    }
}

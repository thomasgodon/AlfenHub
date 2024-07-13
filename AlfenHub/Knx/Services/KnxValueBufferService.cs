using AlfenHub.Alfen.Models;
using AlfenHub.Knx.Extensions;
using AlfenHub.Knx.Models;
using Microsoft.Extensions.Options;

namespace AlfenHub.Knx.Services
{
    internal class KnxValueBufferService : IKnxValueBufferService
    {
        private readonly Dictionary<string, KnxValue> _capabilityKnxValueMapping;
        private readonly object _mappingLock = new();

        public KnxValueBufferService(IOptions<KnxOptions> options)
        {
            _capabilityKnxValueMapping = BuildCapabilityKnxValueMapping(options.Value);
        }


        public IEnumerable<KnxValue> UpdateKnxValues(AlfenData data)
        {
            lock (_mappingLock)
            {
                return UpdateValues(data).Where(m => m is not null).ToList()!;
            }
        }

        private KnxValue? UpdateValue(string capability, byte[] value)
        {
            if (!_capabilityKnxValueMapping.TryGetValue(capability, out var knxAlfenValue))
            {
                return null;
            }

            if (knxAlfenValue.Value is not null)
            {
                if (knxAlfenValue.Value.SequenceEqual(value))
                {
                    return null;
                }
            }

            _capabilityKnxValueMapping[capability].Value = value;
            return _capabilityKnxValueMapping[capability];
        }

        public IReadOnlyDictionary<string, KnxValue> GetKnxValues()
        {
            lock (_mappingLock)
            {
                return _capabilityKnxValueMapping.ToDictionary(key => key.Key, value => value.Value);
            }
        }

        private static Dictionary<string, KnxValue> BuildCapabilityKnxValueMapping(KnxOptions options)
        {
            var alfenData = new Dictionary<string, KnxValue>(options.ReadGroupAddresses.Count);

            foreach (var groupAddressMapping in options.GetReadGroupAddressesFromOptions())
            {
                alfenData.Add(groupAddressMapping.Key, new KnxValue(groupAddressMapping.Value));
            }

            return alfenData;
        }

        private IEnumerable<KnxValue?> UpdateValues(AlfenData alfenData)
        {
            // CurrentPhaseL1 - 14.056 power
            yield return UpdateValue(nameof(AlfenData.Socket1.EnergyMeasurements.CurrentPhaseL1), BitConverter.GetBytes(alfenData.Socket1.EnergyMeasurements.CurrentPhaseL1));
            
        }
    }
}

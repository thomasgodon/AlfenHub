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

            if (knxAlfenValue.Value is not null && knxAlfenValue.Value.SequenceEqual(value))
            {
                return null;
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
            // RealPowerSum - 14.056 power
            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.RealPowerSum)}",
                BitConverter.GetBytes(alfenData.Socket1.EnergyMeasurements.RealPowerSum));

            // SlaveMaxCurrent - 14.019 electric current
            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.SlaveMaxCurrent",
                BitConverter.GetBytes(alfenData.Socket1.StatusAndTransaction.ModbusSlaveMaxCurrent));

            // ActualAppliedMaxCurrent - 14.019 electric current
            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.StatusAndTransaction.ActualAppliedMaxCurrent)}",
                BitConverter.GetBytes(alfenData.Socket1.StatusAndTransaction.ActualAppliedMaxCurrent));

        }
    }
}

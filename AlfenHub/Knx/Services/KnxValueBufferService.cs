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
            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.MeterState)}",
                BitConverter.GetBytes(alfenData.Socket1.EnergyMeasurements.MeterState));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.CurrentSum)}",
                BitConverter.GetBytes(alfenData.Socket1.EnergyMeasurements.CurrentSum));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.PowerFactorSum)}",
                BitConverter.GetBytes(alfenData.Socket1.EnergyMeasurements.PowerFactorSum));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.Frequency)}",
                BitConverter.GetBytes(alfenData.Socket1.EnergyMeasurements.Frequency));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.RealPowerSum)}",
                BitConverter.GetBytes(alfenData.Socket1.EnergyMeasurements.RealPowerSum));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.ApparentPowerSum)}",
                BitConverter.GetBytes(alfenData.Socket1.EnergyMeasurements.ApparentPowerSum));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.ReactivePowerSum)}",
                BitConverter.GetBytes(alfenData.Socket1.EnergyMeasurements.ReactivePowerSum));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.RealEnergyDeliveredSum)}",
                BitConverter.GetBytes((float)alfenData.Socket1.EnergyMeasurements.RealEnergyDeliveredSum));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.RealEnergyConsumedSum)}",
                BitConverter.GetBytes((float)alfenData.Socket1.EnergyMeasurements.RealEnergyConsumedSum));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.EnergyMeasurements.ReactiveEnergySum)}",
                BitConverter.GetBytes((float)alfenData.Socket1.EnergyMeasurements.ReactiveEnergySum));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.SlaveMaxCurrent",
                BitConverter.GetBytes(alfenData.Socket1.StatusAndTransaction.ModbusSlaveMaxCurrent));

            yield return UpdateValue(
                $"{nameof(AlfenData.Socket1)}.{nameof(AlfenData.Socket1.StatusAndTransaction.ActualAppliedMaxCurrent)}",
                BitConverter.GetBytes(alfenData.Socket1.StatusAndTransaction.ActualAppliedMaxCurrent));

        }
    }
}

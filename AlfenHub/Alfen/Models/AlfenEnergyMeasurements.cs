namespace AlfenHub.Alfen.Models
{
    internal class AlfenEnergyMeasurements
    {
        public ushort MeterState { get; init; } = default!;
        public float VoltageL1N { get; init; } = default!;
        public float VoltageL2N { get; init; } = default!;
        public float VoltageL3N { get; init; } = default!;
        public float VoltageL1L2 { get; init; } = default!;
        public float VoltageL2L3 { get; init; } = default!;
        public float VoltageL3L1 { get; init; } = default!;
        public float CurrentN { get; init; } = default!;
        public float CurrentSum { get; init; } = default!;
        public float PowerFactorSum { get; init; } = default!;
        public float Frequency { get; init; } = default!;
        public float RealPowerSum { get; init; } = default!;
        public float ApparentPowerSum { get; init; } = default!;
        public float ReactivePowerSum { get; init; } = default!;
        public double RealEnergyDeliveredSum { get; init; } = default!;
        public double RealEnergyConsumedSum { get; init; } = default!;
        public double ReactiveEnergySum { get; init; } = default!;
    }
}

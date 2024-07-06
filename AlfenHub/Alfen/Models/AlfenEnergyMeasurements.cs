namespace AlfenHub.Alfen.Models
{
    internal class AlfenEnergyMeasurements
    {
        public float VoltageL1N { get; init; } = default!;
        public float VoltageL2N { get; init; } = default!;
        public float VoltageL3N { get; init; } = default!;
        public float VoltageL1L2 { get; init; } = default!;
        public float VoltageL2L3 { get; init; } = default!;
        public float VoltageL3L1 { get; init; } = default!;
        public float CurrentN { get; init; } = default!;
        public float CurrentPhaseL1 { get; init; } = default!;
        public float CurrentPhaseL2 { get; init; } = default!;
        public float CurrentPhaseL3 { get; init; } = default!;
        public float CurrentSum { get; init; } = default!;
        public float PowerFactorPhaseL1 { get; init; } = default!;
        public float PowerFactorPhaseL2 { get; init; } = default!;
        public float PowerFactorPhaseL3 { get; init; } = default!;
        public float Frequency { get; init; } = default!;
        public float RealPowerPhaseL1 { get; init; } = default!;
        public float RealPowerPhaseL2 { get; init; } = default!;
        public float RealPowerPhaseL3 { get; init; } = default!;
        public float RealPowerSum { get; init; } = default!;
    }
}

namespace AlfenHub.Infrastructure.Knx;

/// <summary>
/// Capability strings — the contract between charger readings and KNX group addresses (the keys
/// under <c>KnxOptions.ReadGroupAddresses</c> / <c>WriteGroupAddresses</c> in appsettings).
/// <para>
/// TODO: these are still socket-1-specific string literals coupled to the appsettings keys.
/// Extending to Socket 2 (or formalising capabilities as typed values) means generalising this.
/// </para>
/// </summary>
internal static class KnxCapabilities
{
    public const string MeterState = "Socket1.MeterState";
    public const string CurrentSum = "Socket1.CurrentSum";
    public const string PowerFactorSum = "Socket1.PowerFactorSum";
    public const string Frequency = "Socket1.Frequency";
    public const string RealPowerSum = "Socket1.RealPowerSum";
    public const string ApparentPowerSum = "Socket1.ApparentPowerSum";
    public const string ReactivePowerSum = "Socket1.ReactivePowerSum";
    public const string RealEnergyDeliveredSum = "Socket1.RealEnergyDeliveredSum";
    public const string RealEnergyConsumedSum = "Socket1.RealEnergyConsumedSum";
    public const string ReactiveEnergySum = "Socket1.ReactiveEnergySum";
    public const string Mode3State = "Socket1.Mode3State";
    public const string SlaveMaxCurrent = "Socket1.SlaveMaxCurrent";
    public const string ActualAppliedMaxCurrent = "Socket1.ActualAppliedMaxCurrent";
}

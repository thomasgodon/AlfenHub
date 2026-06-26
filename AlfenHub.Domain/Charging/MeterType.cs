namespace AlfenHub.Domain.Charging;

/// <summary>
/// The energy-meter connection type reported by the socket (Modbus register 305).
/// </summary>
public enum MeterType
{
    Rtu = 0,
    TcpIp = 1,
    Udp = 2,
    P1 = 3,
    Other = 4
}

namespace AlfenHub.Infrastructure.Alfen;

/// <summary>
/// Modbus slave addresses and register ranges from the Alfen Modbus TCP/IP specification.
/// </summary>
internal static class AlfenModbusConstants
{
    public const int StationStatusSlaveAddress = 200;
    public const int StationStatusStartAddress = 1100;
    public const int StationStatusEndAddress = 1105;

    public const int Socket1SlaveAddress = 1;
    public const int Socket2SlaveAddress = 2;
    public const int SocketEnergyMeasurementsStartAddress = 300;
    public const int SocketEnergyMeasurementsEndAddress = 425;
    public const int SocketStatusAndTransactionStartAddress = 1200;
    public const int SocketStatusAndTransactionEndAddress = 1215;

    /// <summary>First register of the Modbus Slave Max Current setpoint (write target).</summary>
    public const int ModbusSlaveMaxCurrentRegister = 1210;
}

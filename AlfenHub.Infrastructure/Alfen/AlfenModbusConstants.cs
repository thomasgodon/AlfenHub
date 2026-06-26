namespace AlfenHub.Infrastructure.Alfen;

/// <summary>
/// Modbus slave addresses and register ranges from the Alfen Modbus TCP/IP specification
/// (NG9xx platform, v2.3). The SCN register block (1400-1431) is intentionally not read.
/// </summary>
internal static class AlfenModbusConstants
{
    // Station-level blocks live on slave address 200.
    public const int StationSlaveAddress = 200;

    public const int ProductIdentificationStartAddress = 100;
    public const int ProductIdentificationEndAddress = 178;

    public const int StationStatusStartAddress = 1100;
    public const int StationStatusEndAddress = 1105;

    // Socket measurement / status blocks live on slave address 1 (and 2 for a dual-socket station).
    public const int Socket1SlaveAddress = 1;
    public const int Socket2SlaveAddress = 2;

    public const int SocketEnergyMeasurementsStartAddress = 300;
    public const int SocketEnergyMeasurementsEndAddress = 425;
    public const int SocketStatusAndTransactionStartAddress = 1200;
    public const int SocketStatusAndTransactionEndAddress = 1215;

    /// <summary>First register of the Modbus Slave Max Current setpoint (read/write, regs 1210-1211).</summary>
    public const int ModbusSlaveMaxCurrentRegister = 1210;

    public const int ProductIdentificationRegisterCount =
        ProductIdentificationEndAddress - ProductIdentificationStartAddress + 1;
    public const int StationStatusRegisterCount =
        StationStatusEndAddress - StationStatusStartAddress + 1;
    public const int SocketEnergyMeasurementsRegisterCount =
        SocketEnergyMeasurementsEndAddress - SocketEnergyMeasurementsStartAddress + 1;
    public const int SocketStatusAndTransactionRegisterCount =
        SocketStatusAndTransactionEndAddress - SocketStatusAndTransactionStartAddress + 1;
}

namespace AlfenHub.Alfen.Modbus.Server
{
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
    }
}

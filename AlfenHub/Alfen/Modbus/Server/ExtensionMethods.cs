namespace AlfenHub.Alfen.Modbus.Server;

internal static class ExtensionMethods
{
    public static ushort[] GetSection(this ushort[] input, int startAddress, int endAddress, int registerStartAddress, int registerEndAddress)
    {
        var sectionLength = endAddress - startAddress + 1;
        if (sectionLength < 1)
        {
            throw new ArgumentException("Start address must be lower than the end address");
        }

        var sectionStart = startAddress - registerStartAddress;
        var section = new ushort[sectionLength + 1];
        Buffer.BlockCopy(input, sectionStart * 2, section, 0, sectionLength);

        return section;
    }

    public static float ToFloat(this ushort[] data)
    {
        var bytes = BitConverter.GetBytes(data.ToArray()[1])
            .Concat(BitConverter.GetBytes(data.ToArray()[0])).ToArray();
        var value = BitConverter.ToSingle(bytes);
        return value is float.NaN ? 0f : value;
    }

    public static ushort ToUshort(this ushort[] data)
    {
        var bytes = BitConverter.GetBytes(data.ToArray()[0]);
        return BitConverter.ToUInt16(bytes);
    }

    public static TimeSpan ToTimespan(this ushort[] data)
    {
        var bytes = BitConverter.GetBytes(data.ToArray()[1])
            .Concat(BitConverter.GetBytes(data.ToArray()[0])).ToArray();
        var value = BitConverter.ToUInt32(bytes);
        return TimeSpan.FromSeconds(value);
    }
}

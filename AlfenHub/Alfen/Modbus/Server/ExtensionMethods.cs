namespace AlfenHub.Alfen.Modbus.Server;

internal static class ExtensionMethods
{
    public static ushort[] GetSection(this ushort[] input, int start, int length)
    {

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
}

using System.Text;
using AlfenHub.Domain.Charging;

namespace AlfenHub.Infrastructure.Alfen;

/// <summary>
/// Decodes/encodes Alfen Modbus holding-register words. Handles the deliberate word-swap and byte
/// ordering used by the Alfen Modbus TCP/IP interface. (Moved verbatim from the old
/// <c>ExtensionMethods</c>; this is the only place that understands the wire format.)
/// </summary>
internal static class ModbusRegisterExtensions
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
        var array = data.ToArray();
        var bytes = BitConverter.GetBytes(array[1])
            .Concat(BitConverter.GetBytes(array[0])).ToArray();
        var value = BitConverter.ToSingle(bytes);
        return value is float.NaN ? 0f : value;
    }

    public static double ToDouble(this ushort[] data)
    {
        var array = data.ToArray();
        var bytes = BitConverter.GetBytes(array[3])
            .Concat(BitConverter.GetBytes(array[2]))
            .Concat(BitConverter.GetBytes(array[1]))
            .Concat(BitConverter.GetBytes(array[0]))
            .ToArray();
        var value = BitConverter.ToDouble(bytes);
        return value is double.NaN ? 0f : value;
    }

    public static ushort[] ToUshortArray(this float value)
    {
        // Convert the float to its byte representation
        var bytes = BitConverter.GetBytes(value);

        // Convert the byte array to two 16-bit registers
        var registers = new ushort[2];
        registers[0] = BitConverter.ToUInt16(bytes, 2);
        registers[1] = BitConverter.ToUInt16(bytes, 0);

        return registers;
    }

    public static ushort ToUshort(this ushort[] data)
    {
        var bytes = BitConverter.GetBytes(data.ToArray()[0]);
        return BitConverter.ToUInt16(bytes);
    }

    public static Mode3State ToMode3State(this ushort[] data)
    {
        var array = data.ToArray();
        var bytes = BitConverter.GetBytes(array[4])
            .Concat(BitConverter.GetBytes(array[3]))
            .Concat(BitConverter.GetBytes(array[2]))
            .Concat(BitConverter.GetBytes(array[1]))
            .Concat(BitConverter.GetBytes(array[0]))
            .ToArray();

        var stringValue = Encoding.UTF8.GetString(bytes).Replace("\0", string.Empty);

        return GetMode3State(stringValue);
    }

    private static Mode3State GetMode3State(string value)
        => value switch
        {
            "A" => Mode3State.A,
            "1B" => Mode3State.B1,
            "2B" => Mode3State.B2,
            "1C" => Mode3State.C1,
            "2C" => Mode3State.C2,
            "1D" => Mode3State.D1,
            "2D" => Mode3State.D2,
            "E" => Mode3State.E,
            _ => Mode3State.F
        };

    public static TimeSpan ToTimespan(this ushort[] data)
    {
        var bytes = BitConverter.GetBytes(data.ToArray()[1])
            .Concat(BitConverter.GetBytes(data.ToArray()[0])).ToArray();
        var value = BitConverter.ToUInt32(bytes);
        return TimeSpan.FromSeconds(value);
    }
}

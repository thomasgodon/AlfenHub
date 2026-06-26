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
        var section = new ushort[sectionLength];
        // BlockCopy counts bytes, so copy sectionLength * 2 bytes (each register is a 16-bit word).
        Buffer.BlockCopy(input, sectionStart * 2, section, 0, sectionLength * 2);

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

    /// <summary>Decodes a single SIGNED16 register.</summary>
    public static short ToShort(this ushort[] data) => unchecked((short)data[0]);

    /// <summary>
    /// Decodes a string register block. Each 16-bit register holds two 8-bit ASCII chars in network
    /// (big-endian) byte order, terminated by a trailing zero. NaN-fill bytes (0xFF) are stripped.
    /// </summary>
    public static string ToAsciiString(this ushort[] data)
    {
        var chars = new List<char>(data.Length * 2);
        foreach (var register in data)
        {
            var high = (byte)(register >> 8);
            var low = (byte)(register & 0xFF);
            foreach (var b in new[] { high, low })
            {
                if (b is 0x00 or 0xFF)
                {
                    continue;
                }

                chars.Add((char)b);
            }
        }

        return new string(chars.ToArray()).Trim();
    }

    /// <summary>Decodes a word-swapped UNSIGNED64 (4 registers, most-significant word first).</summary>
    public static ulong ToULong(this ushort[] data)
    {
        var bytes = BitConverter.GetBytes(data[3])
            .Concat(BitConverter.GetBytes(data[2]))
            .Concat(BitConverter.GetBytes(data[1]))
            .Concat(BitConverter.GetBytes(data[0]))
            .ToArray();
        return BitConverter.ToUInt64(bytes);
    }

    /// <summary>
    /// Decodes a UNSIGNED64 register block expressed in milliseconds (Alfen 0.001s step) into a
    /// <see cref="TimeSpan"/>. Returns <see cref="TimeSpan.Zero"/> for NaN-fill / out-of-range values.
    /// </summary>
    public static TimeSpan ToMilliseconds(this ushort[] data)
    {
        var value = data.ToULong();
        if (value == ulong.MaxValue || value > (ulong)TimeSpan.MaxValue.TotalMilliseconds)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromMilliseconds(value);
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

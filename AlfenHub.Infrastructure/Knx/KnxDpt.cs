namespace AlfenHub.Infrastructure.Knx;

/// <summary>
/// Encodes values into KNX datapoint-type (DPT) byte payloads in <b>wire order</b> (big-endian).
/// The building bus sends these bytes verbatim (no further byte-order manipulation).
/// </summary>
internal static class KnxDpt
{
    /// <summary>DPT 14.xxx — 4-byte IEEE-754 float.</summary>
    public static byte[] Float(float value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    /// <summary>DPT 5.xxx — 1-byte unsigned (0..255), used for small counts/enums/booleans.</summary>
    public static byte[] Byte(int value) => [(byte)Math.Clamp(value, 0, 255)];

    /// <summary>1-byte 0/1 flag (DPT 1.xxx semantics, sent as a single octet).</summary>
    public static byte[] Bool(bool value) => [(byte)(value ? 1 : 0)];

    /// <summary>DPT 7.xxx — 2-byte unsigned, big-endian.</summary>
    public static byte[] UInt16(int value)
    {
        var v = (ushort)Math.Clamp(value, ushort.MinValue, ushort.MaxValue);
        return [(byte)(v >> 8), (byte)(v & 0xFF)];
    }

    /// <summary>DPT 8.xxx — 2-byte signed, big-endian.</summary>
    public static byte[] Int16(int value)
    {
        var v = (short)Math.Clamp(value, short.MinValue, short.MaxValue);
        return [(byte)((v >> 8) & 0xFF), (byte)(v & 0xFF)];
    }

    /// <summary>DPT 13.xxx — 4-byte signed, big-endian (used for durations in seconds).</summary>
    public static byte[] Int32(long value)
    {
        var bytes = BitConverter.GetBytes((int)Math.Clamp(value, int.MinValue, int.MaxValue));
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    /// <summary>DPT 19.001 — 8-byte date + time.</summary>
    public static byte[] DateTime(DateTimeOffset value)
    {
        var bytes = new byte[8];
        bytes[0] = (byte)(value.Year - 1900);
        bytes[1] = (byte)(value.Month & 0x0F);
        bytes[2] = (byte)(value.Day & 0x1F);
        // Day of week: KNX 1 = Monday .. 7 = Sunday; .NET Sunday = 0.
        var dayOfWeek = value.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)value.DayOfWeek;
        bytes[3] = (byte)(((dayOfWeek & 0x07) << 5) | (value.Hour & 0x1F));
        bytes[4] = (byte)(value.Minute & 0x3F);
        bytes[5] = (byte)(value.Second & 0x3F);
        // Flags: NWD (no working-day field) set; all other fields present and valid.
        bytes[6] = 0x20;
        bytes[7] = 0x00;
        return bytes;
    }
}

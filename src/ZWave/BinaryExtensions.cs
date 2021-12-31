namespace ZWave;

internal static class BinaryExtensions
{
    public static sbyte ToInt8(this byte b) => unchecked((sbyte)b);

    public static ushort ToUInt16BE(this ReadOnlySpan<byte> bytes)
    {
        // BitConverter uses the endianness of the machine, so figure out if we have to reverse the bytes.
        if (BitConverter.IsLittleEndian)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            bytes.CopyTo(buffer);
            buffer.Reverse();
            return BitConverter.ToUInt16(buffer);
        }

        return BitConverter.ToUInt16(bytes);
    }

    public static uint ToUInt32BE(this ReadOnlySpan<byte> bytes)
    {
        // BitConverter uses the endianness of the machine, so figure out if we have to reverse the bytes.
        if (BitConverter.IsLittleEndian)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            bytes.CopyTo(buffer);
            buffer.Reverse();
            return BitConverter.ToUInt32(buffer);
        }

        return BitConverter.ToUInt32(bytes);
    }
}

using System.Text;

namespace ZWave.Serial;

public readonly struct DataFrame
{
    internal DataFrame(ReadOnlyMemory<byte> data)
    {
        // No need to do any validations since this in internal. The caller is expected to only pass valid data.

        // Index 0: SOF
        // Index 1: Frame length
        Type = data.Span[2];
        CommandId = data.Span[3];
        CommandParameters = data[4..^1];

        byte expectedChecksum = CalculateChecksum(data.Span);
        int checksum = data.Span[data.Length - 1];
        IsChecksumValid = checksum ==  expectedChecksum;
    }

    public DataFrame(byte type, byte commandId, ReadOnlyMemory<byte> commandParameters)
    {
        Type = type;
        CommandId = commandId;
        CommandParameters = commandParameters;

        // We'll always produce a valid checksum for a data frame we create.
        IsChecksumValid = true;
    }

    public byte Type { get; }

    public byte CommandId { get; }

    public ReadOnlyMemory<byte> CommandParameters { get; }

    public bool IsChecksumValid { get; }

    public void WriteToStream(Stream stream)
    {
        Span<byte> data = stackalloc byte[5 + CommandParameters.Length];
        data[0] = FrameHeader.SOF;
        data[1] = (byte)(data.Length - 2); // Frame length does not include the SOF or Checksum
        data[2] = Type;
        data[3] = CommandId;
        CommandParameters.Span.CopyTo(data[4..]);
        data[data.Length - 1] = CalculateChecksum(data);

        stream.Write(data);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("DataFrame [Type=");
        sb.Append(Type.ToString("x"));
        sb.Append(", CommandId=");
        sb.Append(CommandId.ToString("x"));
        sb.Append(", CommandParameters=");
        for (int i = 0; i < CommandParameters.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }

            sb.Append(CommandParameters.Span[i].ToString("x"));
        }

        if (!IsChecksumValid)
        {
            sb.Append(", InvalidChecksum");
        }

        sb.Append(']');
        return sb.ToString();
    }

    private static byte CalculateChecksum(ReadOnlySpan<byte> data)
    {
        byte checksum = 0xFF;

        // The checksum calculation does not include the SOF or the checksum itself
        for (int i = 1; i < data.Length - 1; i++)
        {
            checksum ^= data[i];
        }

        return checksum;
    }
}

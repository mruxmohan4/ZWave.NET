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
        CommandParameters = data.Slice(4, data.Length - 5);

        // Note that the checksum calculation does not include the SOF or the checksum itself
        int checksumIndex = data.Length - 1;
        byte expectedChecksum = 0xFF;
        for (int i = 1; i < checksumIndex; i++)
        {
            expectedChecksum ^= data.Span[i];
        }

        IsChecksumValid = data.Span[checksumIndex] ==  expectedChecksum;
    }

    public byte Type { get; }

    public byte CommandId { get; }

    public ReadOnlyMemory<byte> CommandParameters { get; }

    public bool IsChecksumValid { get; }
}

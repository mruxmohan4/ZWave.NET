namespace ZWave;

public readonly struct DataFrame
{
    internal DataFrame(byte[] data)
    {
        // No need to do any validations since this in internal. The caller is expected to only pass valid data.

        MessageType = data[2];
        CommandId = data[3];
        CommandParameters = data.AsMemory()[4..];

        // Note that the checksum calculation does not include the SOF or the checksum itself
        int checksumIndex = data.Length - 1;
        byte expectedChecksum = 0xFF;
        for (int i = 1; i < checksumIndex; i++)
        {
            expectedChecksum ^= data[i];
        }

        IsChecksumValid = data[checksumIndex] ==  expectedChecksum;
    }

    public byte MessageType { get; }

    public byte CommandId { get; }

    public ReadOnlyMemory<byte> CommandParameters { get; }

    public bool IsChecksumValid { get; }
}

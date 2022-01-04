namespace ZWave.CommandClasses;

internal struct CommandClassFrame
{
    public CommandClassFrame(ReadOnlyMemory<byte> data)
    {
        if (data.Span.Length < 2)
        {
            throw new ArgumentException("Command class frames must be at least 2 bytes long", nameof(data));
        }

        Data = data;
    }

    public ReadOnlyMemory<byte> Data { get; }

    public CommandClassId CommandClassId => (CommandClassId)Data.Span[0];

    public byte CommandId => Data.Span[1];

    public ReadOnlyMemory<byte> CommandParameters => Data[2..];
}

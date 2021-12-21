using ZWave.Serial;

namespace ZWave.Commands;

internal struct SerialApiSetupRequest : ICommand<SerialApiSetupRequest>
{
    public SerialApiSetupRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SerialApiSetup;

    public DataFrame Frame { get; }

    public static SerialApiSetupRequest GetSupportedCommands()
        => Create(SerialApiSetupSubcommand.GetSupportedCommands, ReadOnlySpan<byte>.Empty);

    public static SerialApiSetupRequest SetTxStatusReport(bool enable)
    {
        Span<byte> subcommandParameters = stackalloc byte[] { (byte)(enable ? 1 : 0) };
        return Create(SerialApiSetupSubcommand.SetTxStatusReport, subcommandParameters);
    }

    private static SerialApiSetupRequest Create(SerialApiSetupSubcommand subcommand, ReadOnlySpan<byte> subcommandParameters)
    {
        Span<byte> commandParameters = stackalloc byte[subcommandParameters.Length + 1];
        commandParameters[0] = (byte)subcommand;
        subcommandParameters.CopyTo(commandParameters[1..]);

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SerialApiSetupRequest(frame);
    }

    public static SerialApiSetupRequest Create(DataFrame frame) => new SerialApiSetupRequest(frame);
}

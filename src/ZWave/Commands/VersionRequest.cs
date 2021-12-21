using ZWave.Serial;

namespace ZWave.Commands;

internal struct VersionRequest : ICommand<VersionRequest>
{
    public VersionRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.Version;

    public DataFrame Frame { get; }

    public static VersionRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new VersionRequest(frame);
    }

    public static VersionRequest Create(DataFrame frame) => new VersionRequest(frame);
}

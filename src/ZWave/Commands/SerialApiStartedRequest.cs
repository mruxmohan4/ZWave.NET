using ZWave.Serial;

namespace ZWave.Commands;

internal struct SerialApiStartedRequest : ICommand<SerialApiStartedRequest>
{
    public SerialApiStartedRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SerialApiStarted;

    public DataFrame Frame { get; }

    public static SerialApiStartedRequest Create(DataFrame frame) => new SerialApiStartedRequest(frame);
}

using ZWave.Serial;

namespace ZWave.Commands;

internal struct SerialApiSoftResetRequest : ICommand<SerialApiSoftResetRequest>
{
    public SerialApiSoftResetRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SerialApiSoftReset;

    public DataFrame Frame { get; }

    public static SerialApiSoftResetRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new SerialApiSoftResetRequest(frame);
    }

    public static SerialApiSoftResetRequest Create(DataFrame frame) => new SerialApiSoftResetRequest(frame);
}

using ZWave.Serial;

namespace ZWave.Commands;

internal struct MemoryGetIdRequest : ICommand<MemoryGetIdRequest>
{
    public MemoryGetIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.MemoryGetId;

    public DataFrame Frame { get; }

    public static MemoryGetIdRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new MemoryGetIdRequest(frame);
    }

    public static MemoryGetIdRequest Create(DataFrame frame) => new MemoryGetIdRequest(frame);
}

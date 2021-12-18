using ZWave.Serial;

namespace ZWave.Commands;

internal struct MemoryGetIdResponse : ICommand
{
    public MemoryGetIdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.MemoryGetId;

    public DataFrame Frame { get; }

    public uint HomeId => Frame.Data.Span[0..4].ToUInt32BE();

    public byte NodeId => Frame.Data.Span[5];

    public static MemoryGetIdResponse Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new MemoryGetIdResponse(frame);
    }
}

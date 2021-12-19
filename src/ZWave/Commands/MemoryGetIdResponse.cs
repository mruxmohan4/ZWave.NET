using ZWave.Serial;

namespace ZWave.Commands;

internal struct MemoryGetIdResponse : ICommand<MemoryGetIdResponse>
{
    public MemoryGetIdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.MemoryGetId;

    public DataFrame Frame { get; }

    public uint HomeId => Frame.CommandParameters.Span[0..4].ToUInt32BE();

    public byte NodeId => Frame.CommandParameters.Span[5];

    public static MemoryGetIdResponse Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new MemoryGetIdResponse(frame);
    }

    public static MemoryGetIdResponse Create(DataFrame frame) => new MemoryGetIdResponse(frame);
}

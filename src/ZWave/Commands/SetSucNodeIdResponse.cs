using ZWave.Serial;

namespace ZWave.Commands;

internal struct SetSucNodeIdResponse : ICommand<SetSucNodeIdResponse>
{
    public SetSucNodeIdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SetSucNodeId;

    public DataFrame Frame { get; }

    public bool WasAccepted => Frame.CommandParameters.Span[0] != 0;

    public static SetSucNodeIdResponse Create(DataFrame frame) => new SetSucNodeIdResponse(frame);
}

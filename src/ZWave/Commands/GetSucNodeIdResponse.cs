using ZWave.Serial;

namespace ZWave.Commands;

internal struct GetSucNodeIdResponse : ICommand<GetSucNodeIdResponse>
{
    public GetSucNodeIdResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetSucNodeId;

    public DataFrame Frame { get; }

    // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
    public byte SucNodeId => Frame.CommandParameters.Span[0];

    public static GetSucNodeIdResponse Create(DataFrame frame) => new GetSucNodeIdResponse(frame);
}

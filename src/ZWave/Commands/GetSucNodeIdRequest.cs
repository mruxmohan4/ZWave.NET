using ZWave.Serial;

namespace ZWave.Commands;

internal struct GetSucNodeIdRequest : ICommand<GetSucNodeIdRequest>
{
    public GetSucNodeIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetSucNodeId;

    public DataFrame Frame { get; }

    public static GetSucNodeIdRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetSucNodeIdRequest(frame);
    }

    public static GetSucNodeIdRequest Create(DataFrame frame) => new GetSucNodeIdRequest(frame);
}

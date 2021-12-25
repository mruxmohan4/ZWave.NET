using ZWave.Serial;

namespace ZWave.Commands;

internal struct SetSucNodeIdRequest : IRequestWithCallback<SetSucNodeIdRequest>
{
    public SetSucNodeIdRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetSucNodeId;

    public static bool ExpectsResponseStatus => true;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[4];

    public static SetSucNodeIdRequest Create(
        byte nodeId,
        bool enableSuc,
        SetSucNodeIdRequestCapabilities capabilities,
        TransmissionOptions transmissionOptions,
        byte sessionId)
    {
        Span<byte> commandParameters = stackalloc byte[5];
        commandParameters[0] = nodeId; // TODO: This may be 16 bits if the node base type is set to 16 bit mode.
        commandParameters[1] = (byte)(enableSuc ? 1 : 0);
        commandParameters[2] = (byte)transmissionOptions;
        commandParameters[3] = (byte)capabilities;
        commandParameters[4] = sessionId;

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetSucNodeIdRequest(frame);
    }

    public static SetSucNodeIdRequest Create(DataFrame frame) => new SetSucNodeIdRequest(frame);
}

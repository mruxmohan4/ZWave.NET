using ZWave.Serial;

namespace ZWave.Commands;

internal struct SetSucNodeIdCallback : ICommand<SetSucNodeIdCallback>
{
    public SetSucNodeIdCallback(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetSucNodeId;

    public DataFrame Frame { get; }

    public byte SessionId => Frame.CommandParameters.Span[0];

    /// <summary>
    /// Indicate the status regarding the configuration of a static/bridge controller to be SUC/SIS node
    /// </summary>
    public byte SetSucNodeIdStatus => Frame.CommandParameters.Span[1];

    public static SetSucNodeIdCallback Create(DataFrame frame) => new SetSucNodeIdCallback(frame);
}

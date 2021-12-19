using ZWave.Serial;

namespace ZWave.Commands;

internal struct GetSerialApiCapabilitiesRequest : ICommand<GetSerialApiCapabilitiesRequest>
{
    public GetSerialApiCapabilitiesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetSerialApiCapabilities;

    public DataFrame Frame { get; }

    public static GetSerialApiCapabilitiesRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetSerialApiCapabilitiesRequest(frame);
    }

    public static GetSerialApiCapabilitiesRequest Create(DataFrame frame) => new GetSerialApiCapabilitiesRequest(frame);
}

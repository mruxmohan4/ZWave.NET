using ZWave.Serial;

namespace ZWave.Commands;

internal struct GetSerialApiCapabilitiesResponse : ICommand<GetSerialApiCapabilitiesResponse>
{
    public GetSerialApiCapabilitiesResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetSerialApiCapabilities;

    public DataFrame Frame { get; }

    public static GetSerialApiCapabilitiesResponse Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetSerialApiCapabilitiesResponse(frame);
    }

    public static GetSerialApiCapabilitiesResponse Create(DataFrame frame) => new GetSerialApiCapabilitiesResponse(frame);
}

using ZWave.Serial;

namespace ZWave.Commands;

internal struct GetControllerCapabilitiesRequest : ICommand<GetControllerCapabilitiesRequest>
{
    public GetControllerCapabilitiesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetControllerCapabilities;

    public DataFrame Frame { get; }

    public static GetControllerCapabilitiesRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetControllerCapabilitiesRequest(frame);
    }

    public static GetControllerCapabilitiesRequest Create(DataFrame frame) => new GetControllerCapabilitiesRequest(frame);
}

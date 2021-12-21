using ZWave.Serial;

namespace ZWave.Commands;

internal struct GetControllerCapabilitiesResponse : ICommand<GetControllerCapabilitiesResponse>
{
    public GetControllerCapabilitiesResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetControllerCapabilities;

    public DataFrame Frame { get; }

    public ControllerCapabilities Capabilities => (ControllerCapabilities)Frame.CommandParameters.Span[0];

    public static GetControllerCapabilitiesResponse Create(DataFrame frame) => new GetControllerCapabilitiesResponse(frame);
}

namespace ZWave.Serial.Commands;

internal struct ResponseStatusResponse : ICommand<ResponseStatusResponse>
{
    public ResponseStatusResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetControllerCapabilities;

    public DataFrame Frame { get; }

    public bool WasRequestAccepted => Frame.CommandParameters.Span[0] != 0;

    public static ResponseStatusResponse Create(DataFrame frame) => new ResponseStatusResponse(frame);
}

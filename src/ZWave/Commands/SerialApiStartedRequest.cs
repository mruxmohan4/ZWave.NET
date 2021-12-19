using ZWave.Serial;

namespace ZWave.Commands;

/// <summary>
/// This command is used by the Z-Wave Module to indicate that it is ready to be operated after a reboot or reset operation
/// </summary>
internal struct SerialApiStartedRequest : ICommand<SerialApiStartedRequest>
{
    public SerialApiStartedRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SerialApiStarted;

    public DataFrame Frame { get; }

    /// <summary>
    /// Gets the event that caused the Z-Wave API module to start
    /// </summary>
    public SerialApiStartedWakeUpReason WakeUpReason => (SerialApiStartedWakeUpReason)Frame.CommandParameters.Span[0];

    /// <summary>
    /// Gets whether the Watchdog is enabled.
    /// </summary>
    public bool WatchdogStarted => Frame.CommandParameters.Span[1] == 0x01;

    /// <summary>
    /// Gets the currently configured listening capabilities configured for the Z-Wave API Module.
    /// </summary>
    public byte DeviceOptionMask => Frame.CommandParameters.Span[2];

    /// <summary>
    /// Gets the currently configured Generic Device Type.
    /// </summary>
    public byte GenericDeviceType => Frame.CommandParameters.Span[3];

    /// <summary>
    /// Gets the currently configured Generic Device Type.
    /// </summary>
    public byte SpecificDeviceType => Frame.CommandParameters.Span[4];

    private byte CommandClassesLength => Frame.CommandParameters.Span[5];

    /// <summary>
    /// Gets the list of supported Command Classes advertised by the Z-Wave API Module upon request.
    /// </summary>
    public ReadOnlyMemory<byte> CommandClasses => Frame.CommandParameters.Slice(6, CommandClassesLength);

    /// <summary>
    /// Gets additional supported protocols by the Z-Wave API module.
    /// </summary>
    public SerialApiStartedSupportedProtocols SupportedProtocols
        => (SerialApiStartedSupportedProtocols)Frame.CommandParameters.Span[6 + CommandClassesLength];

    public static SerialApiStartedRequest Create(DataFrame frame) => new SerialApiStartedRequest(frame);
}

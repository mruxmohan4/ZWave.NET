namespace ZWave;

/// <summary>
/// Identifies a class of error in ZWave.NET
/// </summary>
public enum ZWaveErrorCode
{
    /// <summary>
    /// The driver failed to initialize.
    /// </summary>
    DriverInitializationFailed,

    /// <summary>
    /// The controller failed to initialize.
    /// </summary>
    ControllerInitializationFailed,

    /// <summary>
    /// A ZWAve command failed to send.
    /// </summary>
    CommandSendFailed,

    /// <summary>
    /// A ZWave command failed
    /// </summary>
    CommandFailed,

    /// <summary>
    /// The command class is not supported by the node.
    /// </summary>
    CommandClassNotImplemented,

    /// <summary>
    /// The command is not supported by the node.
    /// </summary>
    CommandNotSupported,

    /// <summary>
    /// A powerlevel test was attempted on a node which doesn't support it.
    /// </summary>
    PowerlevelTestUnsupportedNode,
}

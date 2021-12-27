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
}

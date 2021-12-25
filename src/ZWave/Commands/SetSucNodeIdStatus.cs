namespace ZWave.Commands;

/// <summary>
/// Indicate the status regarding the configuration of a static/bridge controller to be SUC/SIS node
/// </summary>
internal enum SetSucNodeIdStatus : byte
{
    /// <summary>
    /// The process of configuring the static/bridge controller is ended successfully
    /// </summary>
    Succeeded = 0x05,

    /// <summary>
    /// The process of configuring the static/bridge controller is failed.
    /// </summary>
    Failed = 0x06,
}

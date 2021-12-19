namespace ZWave.Commands;

/// <summary>
/// Describes the reason the Z-Wave API Module has been woken up.
/// </summary>
internal enum SerialApiStartedWakeUpReason : byte
{
    /// <summary>
    /// Reset or external interrupt
    /// </summary>
    Reset = 0x00,

    /// <summary>
    /// Timer.
    /// </summary>
    WakeUpTimer = 0x01,

    /// <summary>
    /// Wake Up Beam.
    /// </summary>
    WakeUpBeam = 0x02,

    /// <summary>
    /// Reset triggered by the watchdog.
    /// </summary>
    WatchdogReset = 0x03,

    /// <summary>
    /// External interrupt.
    /// </summary>
    ExternalInterrupt = 0x04,

    /// <summary>
    /// External interrupt.
    /// </summary>
    PowerUp = 0x05,

    /// <summary>
    /// USB Suspend.
    /// </summary>
    UsbSuspend = 0x06,

    /// <summary>
    /// Reset triggered by software.
    /// </summary>
    SoftwareReset = 0x07,

    /// <summary>
    /// Emergency watchdog reset.
    /// </summary>
    EmergencyWatchdogReset = 0x08,

    /// <summary>
    /// Brownout circuit.
    /// </summary>
    BrownoutCircuit = 0x09,

    /// <summary>
    /// Unknown reason.
    /// </summary>
    Unknown = 0xff,
}

namespace ZWave.Commands;

[Flags]
internal enum SerialApiStartedSupportedProtocols : byte
{
    /// <summary>
    /// Indicates if the Z-Wave API module supports Z-Wave
    /// </summary>
    ZWaveLongRange = 0x01,
}

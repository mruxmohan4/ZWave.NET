namespace ZWave.Serial;

public enum CommandId : byte
{
    GetSerialApiCapabilities = 0x07,

    SerialApiSoftReset = 0x08,

    SerialApiStarted = 0x0a,

    GetControllerId = 0x20,
}

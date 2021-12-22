namespace ZWave.Commands;

internal enum SetSucNodeIdRequestCapabilities : byte
{
    /// <summary>
    /// Enable the NodeID server functionality to become a SIS.
    /// </summary>
    SucFuncNodeIdServer = 0x01,
}

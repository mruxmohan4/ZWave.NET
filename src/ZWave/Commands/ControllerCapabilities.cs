namespace ZWave.Commands;

[Flags]
internal enum ControllerCapabilities : byte
{
    /// <summary>
    /// The Z-Wave Module has the secondary controller role
    /// </summary>
    SecondaryController = 1 << 0,

    /// <summary>
    /// The module has been included on another network and did not start the current network
    /// </summary>
    OtherNetwork = 1 << 1,

    /// <summary>
    /// A SIS is present in the current network.
    /// </summary>
    SisIsPresent = 1 << 2,

    // As per spec, bit 3 is to be ignored

    /// <summary>
    /// The module provides the SUC functionality in the current network
    /// </summary>
    SucEnabled = 1 << 4,

    /// <summary>
    /// The module is the only node in the network.
    /// </summary>
    NoNodesIncluded = 1 << 5,
}

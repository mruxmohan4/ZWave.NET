namespace ZWave.Commands;

internal enum VersionLibraryType : byte
{
    /// <summary>
    /// This library is intended for main home controllers, that are typically Primary controllers in a network
    /// </summary>
    StaticController = 0x01,

    /// <summary>
    /// This library is intended for small portable controllers, that are typically secondary controllers or
    /// inclusion controllers in a network
    /// </summary>
    PortableController = 0x02,

    /// <summary>
    /// This library is intended for end nodes.
    /// </summary>
    Enhanced232EndNode = 0x03,

    /// <summary>
    /// This library is intended for end nodes with more limited capabilities than the Enhanced 232 End Node Library.
    /// </summary>
    EndNode = 0x04,

    /// <summary>
    /// This library is intended for controllers nodes used for setup and monitoring of existing networks.
    /// </summary>
    Installer = 0x05,

    /// <summary>
    /// This library is intended for end nodes with routing capabilities.
    /// </summary>
    RoutingEndNode = 0x06,

    /// <summary>
    /// This library is intended for controller nodes that are able to allocate more than 1 NodeID to themselves and use
    /// them for transmitting/receiving frames
    /// </summary>
    BridgeController = 0x07,
}

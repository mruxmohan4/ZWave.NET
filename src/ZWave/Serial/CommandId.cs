namespace ZWave.Serial;

/// <summary>
/// The ids for various Serial API commands.
/// </summary>
/// <remarks>
/// Spec: https://www.silabs.com/documents/public/user-guides/INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
/// </remarks>
internal enum CommandId : byte
{
    /// <summary>
    /// Generate the Node Information frame and to save information about node capabilities.
    /// </summary>
    ApplicationNodeInformation = 0x03,

    /// <summary>
    /// Called when an application command or request has been received from another node.
    /// </summary>
    ApplicationCommandHandler = 0x04,

    /// <summary>
    /// Returns a bitmask containing the capabilities of the controller.
    /// </summary>
    GetControllerCapabilities = 0x05,

    GetSerialApiCapabilities = 0x07,

    SerialApiSoftReset = 0x08,

    SerialApiStarted = 0x0a,

    /// <summary>
    /// Notify the protocol of the command classes it supports using each security key.
    /// This function is only required in slave_routing and slave_enhanced_232 based applications.
    /// </summary>
    ApplicationSecureCommandsSupported = 0x0c,

    /// <summary>
    /// Transmit the data buffer using S2 multicast to a list of Z-Wave Nodes.
    /// </summary>
    SendDataMultiEx = 0x09,

    /// <summary>
    /// Power down the RF when not in use
    /// </summary>
    SetRFReceiveMode = 0x10,

    /// <summary>
    /// Set the SoC in a specified power down mode
    /// </summary>
    SetSleepMode = 0x11,

    /// <summary>
    /// Create and transmit a “Node Information” frame.
    /// </summary>
    SendNodeInformation = 0x12,

    /// <summary>
    /// Transmit the data buffer to a single Z-Wave Node or all Z-Wave Nodes (broadcast).
    /// </summary>
    SendData = 0x13,

    /// <summary>
    /// Transmit the data buffer to a list of Z-Wave Nodes (multicast frame).
    /// </summary>
    SendDataMulti = 0x14,

    /// <summary>
    /// Get the Z-Wave basis API library version.
    /// </summary>
    Version = 0x15,

    /// <summary>
    /// Abort the ongoing transmit started with ZW_SendData or ZW_SendDataMulti.
    /// </summary>
    SendDataAbort = 0x16,

    /// <summary>
    /// Set the power level used for RF transmission
    /// </summary>
    RFPowerLevelSet = 0x17,

    /// <summary>
    /// Returns a random word using the 500 series built-in hardware random number generator 
    /// based on (internal) RF noise(RFRNG).
    /// </summary>
    GetRandomWord = 0x1c,

    /// <summary>
    /// Returns a pseudo-random number
    /// </summary>
    Random = 0x1d,

    /// <summary>
    /// Set the power level locally in the node when finding neighbors.
    /// </summary>
    RFPowerlevelRediscoverySet = 0x1e,

    /// <summary>
    /// Get the Home-ID and Node-ID from the controller.
    /// </summary>
    MemoryGetId = 0x20,

    /// <summary>
    /// Read one byte from the NVM allocated for the application
    /// </summary>
    MemoryGetByte = 0x21,

    /// <summary>
    /// Write one byte to the application area of the NVM.
    /// </summary>
    MemoryPutByte = 0x22,

    /// <summary>
    /// Read a number of bytes from the NVM allocated for the application.
    /// </summary>
    MemoryGetBuffer = 0x23,

    /// <summary>
    /// Copy a number of bytes from a RAM buffer to the application area of the NVM.
    /// </summary>
    MemoryPutBuffer = 0x24,

    /// <summary>
    /// Enables the Auto Program Mode and resets the 500 Series Z-Wave SOC after 7.8ms.
    /// </summary>
    FlashAutoProgSet = 0x27,

    /// <summary>
    /// Read a value from the NVR Flash memory area.
    /// </summary>
    NvrGetValue = 0x28,

    /// <summary>
    /// Get NVM ID from external NVM
    /// </summary>
    NvmGetId = 0x29,

    /// <summary>
    /// Read a number of bytes from external NVM starting from address offset.
    /// </summary>
    NvmExtReadLongBuffer = 0x2a,

    /// <summary>
    /// Write a number of bytes to external NVM starting from address offset.
    /// </summary>
    NvmExtWriteLongBuffer = 0x2b,

    /// <summary>
    /// Read a byte from external NVM at address offset.
    /// </summary>
    NvmExtReadLongByte = 0x2c,

    /// <summary>
    /// Write a byte to external NVM at address offset.
    /// </summary>
    NvmExtWriteLongByte = 0x2d,

    /// <summary>
    /// Clears the protocols internal tx timers
    /// </summary>
    ClearTxTimers = 0x37,

    /// <summary>
    /// Gets the protocol's internal tx timer for the specified channel.
    /// </summary>
    GetTxTimer = 0x38,

    /// <summary>
    /// Clears the current Network Statistics collected by the Z-Wave protocol
    /// </summary>
    ClearNetworkStats = 0x39,

    /// <summary>
    /// Retrieves the current Network Statistics as collected by the Z-Wave protocol.
    /// </summary>
    GetNetworkStats = 0x3a,

    /// <summary>
    /// Returns the most recent background RSSI levels detected
    /// </summary>
    GetBackgroundRSSI = 0x3b,

    /// <summary>
    /// Sets the “Listen Before Talk” threshold that controlles at what RSSI level a Z-Wave node 
    /// will refuse to transmit because of noise.
    /// </summary>
    SetListenBeforeTalkThreshold = 0x3c,

    /// <summary>
    /// Assign static return routes (up to 4) to a Routing Slave or Enhanced 232 Slave node.
    /// </summary>
    AssignReturnRoute = 0x46,

    /// <summary>
    /// Delete all static return routes from a Routing Slave or Enhanced 232 Slave node.
    /// </summary>
    DeleteReturnRoute = 0x47,

    /// <summary>
    /// Update local data structures or to control smart start inclusion
    /// </summary>
    ApplicationControllerUpdate = 0x49,

    /// <summary>
    /// Add a node to a Z-Wave network.
    /// </summary>
    AddNodeToNetwork = 0x4a,

    /// <summary>
    /// Add a controller to the Z-Wave network and transfer the role as primary controller to it
    /// </summary>
    ControllerChange = 0x4d,

    /// <summary>
    /// Assign a application defined Priority Return Route to a routing or an enhanced slave that always will be 
    /// tried as the first return route attempt.
    /// </summary>
    AssignPriorityReturnRoute = 0x4f,

    /// <summary>
    /// Called when node have started inclusion/exclusion through ZW_NetworkLearnModeStart and node has 
    /// been included, excluded or learnmode either failed or timed out.
    /// </summary>
    ApplicationNetworkLearnModeCompleted = 0x50,

    /// <summary>
    /// Notify presence of a SUC/SIS to a Routing Slave or Enhanced 232 Slave.
    /// </summary>
    AssignSucReturnRoute = 0x51,

    /// <summary>
    /// Request a network update from a SUC/SIS controller. Any changes are reported to the application
    /// by calling ApplicationControllerUpdate.
    /// </summary>
    RequestNetworkUpdate = 0x53,

    /// <summary>
    /// Delete the return routes of the SUC/SIS node from a Routing Slave node or Enhanced 232 Slave node.
    /// </summary>
    DeleteSucReturnRoute = 0x55,

    /// <summary>
    /// Assign a application defined Priority SUC Return Route to a routing or an enhanced slave that always 
    /// will be tried as the first return route attempt.
    /// </summary>
    AssignPrioritySucReturnRoute = 0x58,

    /// <summary>
    /// Initiate a Network-Wide Inclusion process
    /// </summary>
    ExploreRequestInclusion = 0x5e,

    /// <summary>
    /// Initiate a Network-Wide Exclusion process
    /// </summary>
    ExploreRequestExclusion = 0x5f,

    /// <summary>
    /// The Firmware Update API provides functionality which together with the SDK supplied ZW_Bootloader 
    /// module and a big enough external NVM makes it possible to implement firmware update
    /// </summary>
    FirmwareUpdate = 0x78,

    /// <summary>
    /// Locks or unlocks response route for a given node ID.
    /// </summary>
    LockRoute = 0x90,

    /// <summary>
    /// Get the route with the highest priority
    /// </summary>
    GetPriorityRoute = 0x92,

    /// <summary>
    /// Returns a bitmask of security keys the node posseses.
    /// </summary>
    GetSecurityKeys = 0x9c,

    /// <summary>
    /// Notify the application of security events.
    /// This function is only required in slave_routing and slave_enhanced_232 based applications.
    /// </summary>
    ApplicationSecurityEvent = 0x9d,

    /// <summary>
    /// Used to set node information for all Virtual Slave Nodes in the embedded module
    /// </summary>
    SerialApiApplicationSlaveNodeInformation = 0xa0,

    /// <summary>
    /// Transmit the data buffer to a list of Z-Wave Nodes (multicast frame).
    /// </summary>
    SendDataMultiBridge = 0xab,

    /// <summary>
    /// Called when an application command has been received from another node to the Bridge
    /// Controller or an existing virtual slave node.
    /// </summary>
    ApplicationCommandHandlerBridge = 0xa8,

    /// <summary>
    /// Transmit the data buffer to a single Z-Wave Node or all Z-Wave Nodes (broadcast).
    /// </summary>
    SendDataBridge = 0xa9,

    /// <summary>
    /// Set the WUT timer interval
    /// </summary>
    SetWutTimeout = 0xb4,

    /// <summary>
    /// Enable the 500 Series Z-Wave SoC built-in watchdog.
    /// </summary>
    WatchdogEnable = 0xb6,

    /// <summary>
    /// Disable the 500 Series Z-Wave SoC built-in watchdog.
    /// </summary>
    WatchdogDisable = 0xb7,

    /// <summary>
    /// Keep the watchdog timer from resetting the 500 Series Z-Wave SoC
    /// </summary>
    WatchdogKick = 0xb8,

    /// <summary>
    /// Set the trigger level for external interrupts
    /// </summary>
    SetExtIntLevel = 0xb9,

    /// <summary>
    /// Get the current power level used in RF transmitting.
    /// </summary>
    RFPowerLevelGet = 0xba,

    /// <summary>
    /// Get the number of neighbors the specified node has registered.
    /// </summary>
    GetNeighborCount = 0xbb,

    /// <summary>
    /// Check if two nodes are marked as being within direct range of each other
    /// </summary>
    AreNodesNeighbors = 0xbc,

    /// <summary>
    /// Get the Z-Wave library type.
    /// </summary>
    TypeLibrary = 0xbd,

    /// <summary>
    /// Send a test frame directly to nodeID without any routing.
    /// </summary>
    SendTestFrame = 0xbe,

    /// <summary>
    /// Request the status of the protocol
    /// </summary>
    GetProtocolStatus = 0xbf,

    /// <summary>
    /// Enable or disable promiscuous mode.
    /// </summary>
    SetPromiscuousMode = 0xd0,

    /// <summary>
    /// Set the maximum interval between SmartStart inclusion requests
    /// </summary>
    NetworkManagementSetMaxInclusionRequestIntervals = 0xd6,
}

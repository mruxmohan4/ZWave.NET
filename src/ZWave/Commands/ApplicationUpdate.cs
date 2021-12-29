using ZWave.Serial;
using ZWave.CommandClasses;
using System;

namespace ZWave.Commands;

/// <summary>
/// Indicates which event has triggered the transmission of thie ApplicationControllerUpdate command.
/// </summary>
enum ApplicationUpdateEvent
{
    /// <summary>
    /// The SIS NodeID has been updated.
    /// </summary>
    SucId = 0x10,

    /// <summary>
    /// A node has been deleted from the network.
    /// </summary>
    DeleteDone = 0x20,

    /// <summary>
    /// A node has been deleted from the network.
    /// </summary>
    NewIdAssigned = 0x40,

    /// <summary>
    /// Another node in the network has requested the Z-Wave API Module to perform a neighbor discovery.
    /// </summary>
    RoutingPending = 0x80,

    /// <summary>
    /// The issued Request Node Information Command has not been acknowledged by the destination
    /// </summary>
    NodeInfoRequestFailed = 0x81,

    /// <summary>
    /// The issued Request Node Information Command has been acknowledged by the destination.
    /// </summary>
    NodeInfoRequestDone = 0x82,

    /// <summary>
    /// Another node sent a NOP Power Command to the Z-Wave API Module.
    /// The host application SHOULD NOT power down the Z-Wave API Module.
    /// </summary>
    NopPowerReceived = 0x83,

    /// <summary>
    /// A Node Information Frame has been received as unsolicited frame or in response to a Request Node Information Command
    /// </summary>
    NodeInfoReceived = 0x84,

    /// <summary>
    /// A SmartStart Prime Command has been received using the Z-Wave protocol.
    /// </summary>
    NodeInfoSmartStartHomeIdReceived = 0x85,

    /// <summary>
    /// A SmartStart Included Node Information Frame has been received (using either Z-Wave or Z-Wave Long Range protocol).
    /// </summary>
    IncludedNodeInfoReceived = 0x86,

    /// <summary>
    /// A SmartStart Prime Command has been received using the Z-Wave Long Range protocol.
    /// </summary>
    NodeInfoSmartStartHomeIdReceivedLongRange = 0x87,
}

internal struct ApplicationControllerUpdateNodeInfoReceived : ICommand<ApplicationControllerUpdateNodeInfoReceived>
{
    public ApplicationControllerUpdateNodeInfoReceived(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ApplicationUpdate;

    public DataFrame Frame { get; }

    public ApplicationUpdateEvent Event => (ApplicationUpdateEvent)Frame.CommandParameters.Span[0];

    public byte NodeId => Frame.CommandParameters.Span[1];

    public byte BasicDeviceClass => Frame.CommandParameters.Span[3];

    public byte GenericDeviceClass => Frame.CommandParameters.Span[4];

    public byte SpecificDeviceClass => Frame.CommandParameters.Span[5];

    /// <summary>
    /// The list of non-secure supported Command Classes by the remote node.
    /// </summary>
    public IReadOnlyList<CommandClassId> SupportedCommandClasses
    {
        get
        {
            byte length = Frame.CommandParameters.Span[2];
            ReadOnlySpan<byte> allCommandClasses = Frame.CommandParameters.Span.Slice(6, length);

            int supportControlMark = allCommandClasses.IndexOf((byte)CommandClassId.SupportControlMark);

            // If the support control mark isn't in the list, they're all supported-only.
            ReadOnlySpan<byte> supportedCommandClassBytes = supportControlMark == -1
                ? allCommandClasses
                : allCommandClasses.Slice(0, supportControlMark);

            var supportedCommandClasses = new CommandClassId[supportedCommandClassBytes.Length];
            for (int i = 0; i < supportedCommandClassBytes.Length; i++)
            {
                supportedCommandClasses[i] = (CommandClassId)supportedCommandClassBytes[i];
            }

            return supportedCommandClasses;
        }
    }

    /// <summary>
    /// The list of non-secure controlled Command Classes by the remote node.
    /// </summary>
    public IReadOnlyList<CommandClassId> ControlledCommandClasses
    {
        get
        {
            byte length = Frame.CommandParameters.Span[2];
            ReadOnlySpan<byte> allCommandClasses = Frame.CommandParameters.Span.Slice(6, length);

            int supportControlMark = allCommandClasses.IndexOf((byte)CommandClassId.SupportControlMark);

            // If the support control mark isn't in the list, none are controlled.
            ReadOnlySpan<byte> controlledCommandClassBytes = supportControlMark == -1
                ? ReadOnlySpan<byte>.Empty
                : allCommandClasses.Slice(supportControlMark + 1);

            var controlledCommandClasses = new CommandClassId[controlledCommandClassBytes.Length];
            for (int i = 0; i < controlledCommandClassBytes.Length; i++)
            {
                controlledCommandClasses[i] = (CommandClassId)controlledCommandClassBytes[i];
            }

            return controlledCommandClasses;
        }
    }

    public static ApplicationControllerUpdateNodeInfoReceived Create(DataFrame frame) => new ApplicationControllerUpdateNodeInfoReceived(frame);
}
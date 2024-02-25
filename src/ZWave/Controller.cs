﻿using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;
using ZWave.Serial.Commands;

namespace ZWave;

public sealed class Controller
{
    private readonly ILogger _logger;

    private readonly IDriver _driver;

    private readonly Dictionary<byte, Node> _nodes = new Dictionary<byte, Node>();

    public Controller(
        ILogger logger,
        IDriver driver)
    {
        _logger = logger;
        _driver = driver;
    }

    public uint HomeId { get; private set; }

    public byte NodeId { get; private set; }

    public Version? FirmwareVersion { get; private set; }

    public ushort ManufacturerId { get; private set; }

    public ushort ProductType { get; private set; }

    public ushort ProductId { get; private set; }

    public HashSet<CommandId>? SupportedCommandIds { get; private set; }

    public string? LibraryVersion { get; private set; }

    public LibraryType LibraryType { get; private set; }

    public HashSet<SerialApiSetupSubcommand>? SupportedSerialApiSetupSubcommands { get; private set; }

    public byte SucNodeId { get; private set; }

    public byte ApiVersion { get; private set; }

    public byte ChipType { get; private set; }

    public byte ChipVersion { get; private set; }

    public bool IsPrimary { get; private set; }

    public IReadOnlyDictionary<byte, Node> Nodes => _nodes;

    public async Task IdentifyAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogControllerIdentifying();

            var memoryGetIdRequest = MemoryGetIdRequest.Create();
            MemoryGetIdResponse memoryGetIdResponse = await _driver.SendCommandAsync<MemoryGetIdRequest, MemoryGetIdResponse>(
                memoryGetIdRequest,
                cancellationToken).ConfigureAwait(false);
            HomeId = memoryGetIdResponse.HomeId;
            NodeId = memoryGetIdResponse.NodeId;
            _logger.LogControllerIdentity(HomeId, NodeId);

            var getSerialCapabilitiesRequest = GetSerialApiCapabilitiesRequest.Create();
            GetSerialApiCapabilitiesResponse getSerialCapabilitiesResponse = await _driver.SendCommandAsync<GetSerialApiCapabilitiesRequest, GetSerialApiCapabilitiesResponse>(
                getSerialCapabilitiesRequest,
                cancellationToken).ConfigureAwait(false);

            // Although the docs refer to this as the serial API verison, it's really the controller device's firmware version.
            FirmwareVersion = new Version(getSerialCapabilitiesResponse.SerialApiVersion, getSerialCapabilitiesResponse.SerialApiRevision);
            ManufacturerId = getSerialCapabilitiesResponse.ManufacturerId;
            ProductType = getSerialCapabilitiesResponse.ManufacturerProductType;
            ProductId = getSerialCapabilitiesResponse.ManufacturerProductId;
            SupportedCommandIds = getSerialCapabilitiesResponse.SupportedCommandIds;
            _logger.LogSerialApiCapabilities(
                FirmwareVersion,
                ManufacturerId,
                ProductType,
                ProductId,
                FormatCommandIds(SupportedCommandIds));

            var versionRequest = GetLibraryVersionRequest.Create();
            GetLibraryVersionResponse versionResponse = await _driver.SendCommandAsync<GetLibraryVersionRequest, GetLibraryVersionResponse>(
                versionRequest,
                cancellationToken).ConfigureAwait(false);
            LibraryVersion = versionResponse.LibraryVersion;
            LibraryType = versionResponse.LibraryType;
            _logger.LogControllerLibraryVersion(LibraryVersion, LibraryType);

            var getControllerCapabilitiesRequest = GetControllerCapabilitiesRequest.Create();
            GetControllerCapabilitiesResponse getControllerCapabilitiesResponse = await _driver.SendCommandAsync<GetControllerCapabilitiesRequest, GetControllerCapabilitiesResponse>(
                getControllerCapabilitiesRequest,
                cancellationToken).ConfigureAwait(false);
            ControllerCapabilities controllerCapabilities = getControllerCapabilitiesResponse.Capabilities;
            IsPrimary = !controllerCapabilities.HasFlag(ControllerCapabilities.SecondaryController);
            _logger.LogControllerCapabilities(controllerCapabilities);

            if (SupportedCommandIds.Contains(SerialApiSetupRequest.CommandId))
            {
                var getSupportedSetupCommandsRequest = SerialApiSetupRequest.GetSupportedCommands();
                SerialApiSetupGetSupportedCommandsResponse getSupportedSetupCommandsResponse = await _driver.SendCommandAsync<SerialApiSetupRequest, SerialApiSetupGetSupportedCommandsResponse>(
                    getSupportedSetupCommandsRequest,
                    cancellationToken).ConfigureAwait(false);

                // The command was supported and this subcommand should always be supported, so this should never happen in practice.
                if (!getSupportedSetupCommandsResponse.WasSubcommandSupported)
                {
                    throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "SerialApiSetup.GetSupportedCommands was not supported");
                }

                SupportedSerialApiSetupSubcommands = getSupportedSetupCommandsResponse.SupportedSubcommands;
            }
            else
            {
                SupportedSerialApiSetupSubcommands = new HashSet<SerialApiSetupSubcommand>(0);
            }

            _logger.LogControllerSupportedSerialApiSetupSubcommands(FormatSerialApiSetupSubcommands(SupportedSerialApiSetupSubcommands));

            if (SupportedSerialApiSetupSubcommands.Contains(SerialApiSetupSubcommand.SetTxStatusReport))
            {
                var setTxStatusReportRequest = SerialApiSetupRequest.SetTxStatusReport(enable: true);
                SerialApiSetupSetTxStatusReportResponse setTxStatusReportResponse = await _driver.SendCommandAsync<SerialApiSetupRequest, SerialApiSetupSetTxStatusReportResponse>(
                    setTxStatusReportRequest,
                    cancellationToken).ConfigureAwait(false);

                // We checked that this was supported, so this should never happen in practice.
                if (!setTxStatusReportResponse.WasSubcommandSupported)
                {
                    throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "SerialApiSetup.SetTxStatusReport was not supported");
                }

                _logger.LogEnableTxStatusReport(setTxStatusReportResponse.Success);
            }

            var getSucNodeIdRequest = GetSucNodeIdRequest.Create();
            GetSucNodeIdResponse getSucNodeIdResponse = await _driver.SendCommandAsync<GetSucNodeIdRequest, GetSucNodeIdResponse>(
                getSucNodeIdRequest,
                cancellationToken).ConfigureAwait(false);
            SucNodeId = getSucNodeIdResponse.SucNodeId;
            _logger.LogControllerSucNodeId(SucNodeId);

            // If there is no SUC/SIS and we're not a SUC or secondard controller, promote ourselves
            if (SucNodeId == 0
                && !controllerCapabilities.HasFlag(ControllerCapabilities.SecondaryController)
                && !controllerCapabilities.HasFlag(ControllerCapabilities.SucEnabled)
                && !controllerCapabilities.HasFlag(ControllerCapabilities.SisIsPresent))
            {
                var setSucNodeIdRequest = SetSucNodeIdRequest.Create(
                    SucNodeId,
                    enableSuc: true,
                    SetSucNodeIdRequestCapabilities.SucFuncNodeIdServer,
                    TransmissionOptions.ACK | TransmissionOptions.AutoRoute | TransmissionOptions.Explore,
                    _driver.GetNextSessionId());
                SetSucNodeIdCallback setSucNodeIdCallback = await _driver.SendCommandExpectingCallbackAsync<SetSucNodeIdRequest, SetSucNodeIdCallback>(
                    setSucNodeIdRequest,
                    cancellationToken).ConfigureAwait(false);

                if (setSucNodeIdCallback.SetSucNodeIdStatus != SetSucNodeIdStatus.Succeeded)
                {
                    throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "SetSucNodeId failed");
                }

                SucNodeId = NodeId;
            }

            var getInitDataRequest = GetInitDataRequest.Create();
            GetInitDataResponse getInitDataResponse = await _driver.SendCommandAsync<GetInitDataRequest, GetInitDataResponse>(
                getInitDataRequest,
                cancellationToken).ConfigureAwait(false);
            ApiVersion = getInitDataResponse.ApiVersion;
            GetInitDataCapabilities apiCapabilities = getInitDataResponse.ApiCapabilities;
            IsPrimary = !apiCapabilities.HasFlag(GetInitDataCapabilities.SecondaryController);
            ChipType = getInitDataResponse.ChipType;
            ChipVersion = getInitDataResponse.ChipVersion;

            HashSet<byte> nodeIds = getInitDataResponse.NodeIds;
            foreach (byte nodeId in nodeIds)
            {
                _nodes.Add(nodeId, new Node(nodeId, _driver, _logger));
            }

            _logger.LogInitData(
                ApiVersion,
                apiCapabilities,
                ChipType,
                ChipVersion,
                FormatNodeIds(nodeIds));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "Failed to initialize the controller", ex);
        }
    }

    private static string FormatCommandIds(HashSet<CommandId> commandIds)
    {
        int literalLength = commandIds.Count * 2;
        int formattedCount = commandIds.Count;

        var handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        handler.AppendLiteral("[");
        bool isFirst = true;
        foreach (CommandId commandId in commandIds)
        {
            if (!isFirst)
            {
                handler.AppendLiteral(", ");
            }

            isFirst = false;

            handler.AppendFormatted(commandId);
        }

        handler.AppendLiteral("]");
        return handler.ToStringAndClear();
    }

    private static string FormatSerialApiSetupSubcommands(HashSet<SerialApiSetupSubcommand> subcommands)
    {
        int literalLength = subcommands.Count * 2;
        int formattedCount = subcommands.Count;

        var handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        handler.AppendLiteral("[");
        bool isFirst = true;
        foreach (SerialApiSetupSubcommand subcommand in subcommands)
        {
            if (!isFirst)
            {
                handler.AppendLiteral(", ");
            }

            isFirst = false;

            handler.AppendFormatted(subcommand);
        }

        handler.AppendLiteral("]");
        return handler.ToStringAndClear();
    }

    private static string FormatNodeIds(HashSet<byte> nodeIds)
    {
        int literalLength = nodeIds.Count * 2;
        int formattedCount = nodeIds.Count;

        var handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        handler.AppendLiteral("[");
        bool isFirst = true;
        foreach (byte nodeId in nodeIds)
        {
            if (!isFirst)
            {
                handler.AppendLiteral(", ");
            }

            isFirst = false;

            handler.AppendFormatted(nodeId);
        }

        handler.AppendLiteral("]");
        return handler.ToStringAndClear();
    }
}

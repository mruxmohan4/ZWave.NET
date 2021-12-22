using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

using ZWave.Commands;
using ZWave.Serial;

namespace ZWave;

internal sealed class Controller
{
    private readonly ILogger _logger;

    private readonly Driver _driver;

    public Controller(
        ILogger logger,
        Driver driver)
    {
        _logger = logger;
        _driver = driver;
    }

    public uint HomeId { get; private set; }

    public byte NodeId { get; private set; }

    public byte SerialApiVersion { get; private set; }

    public byte SerialApiRevision { get; private set; }

    public ushort ManufacturerId { get; private set; }

    public ushort ProductType { get; private set; }

    public ushort ProductId { get; private set; }

    public HashSet<CommandId>? SupportedCommandIds { get; private set; }

    public string? LibraryVersion { get; private set; }

    public VersionLibraryType LibraryType { get; private set; }

    public ControllerCapabilities Capabilities { get; private set; }

    public HashSet<SerialApiSetupSubcommand>? SupportedSerialApiSetupSubcommands { get; private set; }

    public byte SucNodeId { get; private set; }

    public async Task IdentifyAsync(CancellationToken cancellationToken)
    {
        var memoryGetIdRequest = MemoryGetIdRequest.Create();
        MemoryGetIdResponse? memoryGetIdResponse = await _driver.SendRequestCommandAsync<MemoryGetIdRequest, MemoryGetIdResponse>(
            memoryGetIdRequest,
            cancellationToken).ConfigureAwait(false);
        if (memoryGetIdResponse == null)
        {
            throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "MemoryGetId request timed out");
        }

        HomeId = memoryGetIdResponse.Value.HomeId;
        NodeId = memoryGetIdResponse.Value.NodeId;
        _logger.LogControllerIdentity(HomeId, NodeId);

        var getSerialCapabilitiesRequest = GetSerialApiCapabilitiesRequest.Create();
        GetSerialApiCapabilitiesResponse? getSerialCapabilitiesResponse = await _driver.SendRequestCommandAsync<GetSerialApiCapabilitiesRequest, GetSerialApiCapabilitiesResponse>(
            getSerialCapabilitiesRequest,
            cancellationToken).ConfigureAwait(false);
        if (getSerialCapabilitiesResponse == null)
        {
            throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "GetSerialApiCapabilities request timed out");
        }

        SerialApiVersion = getSerialCapabilitiesResponse.Value.SerialApiVersion;
        SerialApiRevision = getSerialCapabilitiesResponse.Value.SerialApiRevision;
        ManufacturerId = getSerialCapabilitiesResponse.Value.ManufacturerId;
        ProductType = getSerialCapabilitiesResponse.Value.ManufacturerProductType;
        ProductId = getSerialCapabilitiesResponse.Value.ManufacturerProductId;
        SupportedCommandIds = getSerialCapabilitiesResponse.Value.SupportedCommandIds;

        _logger.LogSerialApiCapabilities(
            SerialApiVersion,
            SerialApiRevision,
            ManufacturerId,
            ProductType,
            ProductId,
            FormatCommandIds(SupportedCommandIds));

        var versionRequest = VersionRequest.Create();
        VersionResponse? versionResponse = await _driver.SendRequestCommandAsync<VersionRequest, VersionResponse>(
            versionRequest,
            cancellationToken).ConfigureAwait(false);
        if (versionResponse == null)
        {
            throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "Version request timed out");
        }

        LibraryVersion = versionResponse.Value.LibraryVersion;
        LibraryType = versionResponse.Value.LibraryType;
        _logger.LogControllerLibraryVersion(LibraryVersion, LibraryType);

        var getControllerCapabilitiesRequest = GetControllerCapabilitiesRequest.Create();
        GetControllerCapabilitiesResponse? getControllerCapabilitiesResponse = await _driver.SendRequestCommandAsync<GetControllerCapabilitiesRequest, GetControllerCapabilitiesResponse>(
            getControllerCapabilitiesRequest,
            cancellationToken).ConfigureAwait(false);
        if (getControllerCapabilitiesResponse == null)
        {
            throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "GetControllerCapabilities request timed out");
        }

        Capabilities = getControllerCapabilitiesResponse.Value.Capabilities;
        _logger.LogControllerCapabilities(Capabilities);

        if (SupportedCommandIds.Contains(SerialApiSetupRequest.CommandId))
        {
            var getSupportedSetupCommandsRequest = SerialApiSetupRequest.GetSupportedCommands();
            SerialApiSetupGetSupportedCommandsResponse? getSupportedSetupCommandsResponse = await _driver.SendRequestCommandAsync<SerialApiSetupRequest, SerialApiSetupGetSupportedCommandsResponse>(
                getSupportedSetupCommandsRequest,
                cancellationToken).ConfigureAwait(false);
            if (getSupportedSetupCommandsResponse == null)
            {
                throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "SerialApiSetup.GetSupportedCommands request timed out");
            }

            // The command was supported and this subcommand should always be supported, so this should never happen in practice.
            if (!getSupportedSetupCommandsResponse.Value.WasSubcommandSupported)
            {
                throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "SerialApiSetup.GetSupportedCommands was not supported");
            }

            SupportedSerialApiSetupSubcommands = getSupportedSetupCommandsResponse.Value.SupportedSubcommands;
        }
        else
        {
            SupportedSerialApiSetupSubcommands = new HashSet<SerialApiSetupSubcommand>(0);
        }

        _logger.LogControllerSupportedSerialApiSetupSubcommands(FormatSerialApiSetupSubcommands(SupportedSerialApiSetupSubcommands));

        if (SupportedSerialApiSetupSubcommands.Contains(SerialApiSetupSubcommand.SetTxStatusReport))
        {
            var setTxStatusReportRequest = SerialApiSetupRequest.SetTxStatusReport(enable: true);
            SerialApiSetupSetTxStatusReportResponse? setTxStatusReportResponse = await _driver.SendRequestCommandAsync<SerialApiSetupRequest, SerialApiSetupSetTxStatusReportResponse>(
                setTxStatusReportRequest,
                cancellationToken).ConfigureAwait(false);
            if (setTxStatusReportResponse == null)
            {
                throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "SerialApiSetup.SetTxStatusReport request timed out");
            }

            // We checked that this was supported, so this should never happen in practice.
            if (!setTxStatusReportResponse.Value.WasSubcommandSupported)
            {
                throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "SerialApiSetup.SetTxStatusReport was not supported");
            }

            _logger.LogEnableTxStatusReport(setTxStatusReportResponse.Value.Success);
        }

        var getSucNodeIdRequest = GetSucNodeIdRequest.Create();
        GetSucNodeIdResponse? getSucNodeIdResponse = await _driver.SendRequestCommandAsync<GetSucNodeIdRequest, GetSucNodeIdResponse>(
            getSucNodeIdRequest,
            cancellationToken).ConfigureAwait(false);
        if (getSucNodeIdResponse == null)
        {
            throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "GetSucNodeId request timed out");
        }

        SucNodeId = getSucNodeIdResponse.Value.SucNodeId;
        _logger.LogControllerSucNodeId(SucNodeId);

        // If there is no SUC/SIS and we're not a SUC or secondard controller, promote ourselves
        if (SucNodeId == 0
            && !Capabilities.HasFlag(ControllerCapabilities.SecondaryController)
            && !Capabilities.HasFlag(ControllerCapabilities.SucEnabled)
            && !Capabilities.HasFlag(ControllerCapabilities.SisIsPresent))
        {
            var setSucNodeIdSessionId = _driver.GetNextSessionId();
            var setSucNodeIdRequest = SetSucNodeIdRequest.Create(
                SucNodeId,
                enableSuc: true,
                SetSucNodeIdRequestCapabilities.SucFuncNodeIdServer,
                TransmissionOptions.ACK | TransmissionOptions.AutoRoute | TransmissionOptions.Explore,
                setSucNodeIdSessionId);
            (SetSucNodeIdResponse, SetSucNodeIdRequest)? responseAndCallback = await _driver.SendRequestCommandWithCallbackAsync<SetSucNodeIdRequest, SetSucNodeIdResponse>(
                setSucNodeIdRequest,
                cancellationToken).ConfigureAwait(false);
            if (!responseAndCallback.HasValue)
            {
                throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "SetSucNodeId request timed out");
            }

            (SetSucNodeIdResponse response, SetSucNodeIdRequest callback) = responseAndCallback.Value;
            // TODO: Use these
        }
    }

    private string FormatCommandIds(HashSet<CommandId> commandIds)
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

    private string FormatSerialApiSetupSubcommands(HashSet<SerialApiSetupSubcommand> subcommands)
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
}

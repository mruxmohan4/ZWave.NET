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

    public HashSet<CommandId>? SupportedCommandIds { get; private set; }

    public HashSet<SerialApiSetupSubcommand>? SupportedSerialApiSetupSubcommands { get; private set; }

    public HashSet<byte>? NodeIds { get; private set; }

    public async Task IdentifyAsync(CancellationToken cancellationToken)
    {
        try
        {
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

            byte serialApiVersion = getSerialCapabilitiesResponse.SerialApiVersion;
            byte serialApiRevision = getSerialCapabilitiesResponse.SerialApiRevision;
            ushort manufacturerId = getSerialCapabilitiesResponse.ManufacturerId;
            ushort productType = getSerialCapabilitiesResponse.ManufacturerProductType;
            ushort productId = getSerialCapabilitiesResponse.ManufacturerProductId;
            SupportedCommandIds = getSerialCapabilitiesResponse.SupportedCommandIds;
            _logger.LogSerialApiCapabilities(
                serialApiVersion,
                serialApiRevision,
                manufacturerId,
                productType,
                productId,
                FormatCommandIds(SupportedCommandIds));

            var versionRequest = GetLibraryVersionRequest.Create();
            GetLibraryVersionResponse versionResponse = await _driver.SendCommandAsync<GetLibraryVersionRequest, GetLibraryVersionResponse>(
                versionRequest,
                cancellationToken).ConfigureAwait(false);
            string libraryVersion = versionResponse.LibraryVersion;
            VersionLibraryType libraryType = versionResponse.LibraryType;
            _logger.LogControllerLibraryVersion(libraryVersion, libraryType);

            var getControllerCapabilitiesRequest = GetControllerCapabilitiesRequest.Create();
            GetControllerCapabilitiesResponse getControllerCapabilitiesResponse = await _driver.SendCommandAsync<GetControllerCapabilitiesRequest, GetControllerCapabilitiesResponse>(
                getControllerCapabilitiesRequest,
                cancellationToken).ConfigureAwait(false);
            ControllerCapabilities controllerCapabilities = getControllerCapabilitiesResponse.Capabilities;
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
            var sucNodeId = getSucNodeIdResponse.SucNodeId;
            _logger.LogControllerSucNodeId(sucNodeId);

            // If there is no SUC/SIS and we're not a SUC or secondard controller, promote ourselves
            if (sucNodeId == 0
                && !controllerCapabilities.HasFlag(ControllerCapabilities.SecondaryController)
                && !controllerCapabilities.HasFlag(ControllerCapabilities.SucEnabled)
                && !controllerCapabilities.HasFlag(ControllerCapabilities.SisIsPresent))
            {
                var setSucNodeIdRequest = SetSucNodeIdRequest.Create(
                    sucNodeId,
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
            }

            var getInitDataRequest = GetInitDataRequest.Create();
            GetInitDataResponse getInitDataResponse = await _driver.SendCommandAsync<GetInitDataRequest, GetInitDataResponse>(
                getInitDataRequest,
                cancellationToken).ConfigureAwait(false);
            byte apiVersion = getInitDataResponse.ApiVersion;
            GetInitDataCapabilities apiCapabilities = getInitDataResponse.ApiCapabilities;
            byte chipType = getInitDataResponse.ChipType;
            byte chipVersion = getInitDataResponse.ChipVersion;
            NodeIds = getInitDataResponse.NodeIds;
            _logger.LogInitData(
                apiVersion,
                apiCapabilities,
                chipType,
                chipVersion,
                FormatNodeIds(NodeIds));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ZWaveException(ZWaveErrorCode.ControllerInitializationFailed, "Failed to initialize the controller");
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

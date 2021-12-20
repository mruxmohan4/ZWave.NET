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

        _logger.LogControllerCapabilities(
            SerialApiVersion,
            SerialApiRevision,
            ManufacturerId,
            ProductType,
            ProductId,
            FormatCommandIds(SupportedCommandIds));
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
}

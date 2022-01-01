using Microsoft.Extensions.Logging;
using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave;

internal static partial class Logging
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "ZWave Serial port `{portName}` opened")]
    public static partial void LogSerialApiPortOpened(this ILogger logger, string portName);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Trace,
        Message = "ZWave Serial port `{portName}` closed")]
    public static partial void LogSerialApiPortClosed(this ILogger logger, string portName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "Skipped {numBytes} bytes of invalid data from the Serial port")]
    public static partial void LogSerialApiSkippedBytes(this ILogger logger, long numBytes);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Trace,
        Message = "Received partial Serial API data frame with length {length}")]
    public static partial void LogSerialApiPartialDataFrameReceived(this ILogger logger, long length);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Trace,
        Message = "Received Serial API frame: {frame}")]
    public static partial void LogSerialApiFrameReceived(this ILogger logger, Frame frame);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Trace,
        Message = "Received invalid Serial API data frame: {frame}")]
    public static partial void LogSerialApiInvalidDataFrame(this ILogger logger, DataFrame frame);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Trace,
        Message = "Sent Serial API frame: {frame}")]
    public static partial void LogSerialApiFrameSent(this ILogger logger, Frame frame);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Trace,
        Message = "Sent Serial API data frame: {frame}")]
    public static partial void LogSerialApiDataFrameSent(this ILogger logger, DataFrame frame);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Trace,
        Message = "Received Serial API data frame with unknown type `{dataFrameType}`")]
    public static partial void LogSerialApiDataFrameUnknownType(this ILogger logger, DataFrameType dataFrameType);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "Driver initialization sequence starting")]
    public static partial void LogDriverInitializing(this ILogger logger);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Performing soft reset")]
    public static partial void LogSoftReset(this ILogger logger);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "Driver initialization sequence complete")]
    public static partial void LogDriverInitialized(this ILogger logger);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Debug,
        Message = "Identifying controller")]
    public static partial void LogControllerIdentifying(this ILogger logger);

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Information,
        Message = "Controller identity:\n" +
        "Home ID = {homeId}\n" +
        "Node ID = {nodeId}")]
    public static partial void LogControllerIdentity(this ILogger logger, uint homeId, byte nodeId);

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Information,
        Message = "Serial API capabilities:\n" +
        "Serial API Version = {serialApiVersion}\n" +
        "Serial API Revision = {serialApiRevision}\n" +
        "Manufacturer ID = {manufacturerId}\n" +
        "Product type = {productType}\n" +
        "Product ID = {productId}\n" +
        "Supported Commands = {supportedCommands}")]
    public static partial void LogSerialApiCapabilities(
        this ILogger logger,
        byte serialApiVersion,
        byte serialApiRevision,
        ushort manufacturerId,
        ushort productType,
        ushort productId,
        string supportedCommands);

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Information,
        Message = "Controller library:\n" +
        "Library version = {libraryVersion}\n" +
        "Library type = {libraryType}")]
    public static partial void LogControllerLibraryVersion(this ILogger logger, string libraryVersion, LibraryType libraryType);

    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Information,
        Message = "Controller capabilities: {controllerCapabilities}")]
    public static partial void LogControllerCapabilities(this ILogger logger, ControllerCapabilities controllerCapabilities);

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Information,
        Message = "Supported Serial API Setup subcommands: {supportedSubcommands}")]
    public static partial void LogControllerSupportedSerialApiSetupSubcommands(this ILogger logger, string supportedSubcommands);

    [LoggerMessage(
        EventId = 18,
        Level = LogLevel.Debug,
        Message = "Enabling TX status report success: {success}")]
    public static partial void LogEnableTxStatusReport(this ILogger logger, bool success);

    [LoggerMessage(
        EventId = 19,
        Level = LogLevel.Debug,
        Message = "SUC Node Id: {sucNodeId}")]
    public static partial void LogControllerSucNodeId(this ILogger logger, byte sucNodeId);

    [LoggerMessage(
        EventId = 20,
        Level = LogLevel.Information,
        Message = "Init data:\n" +
        "API Version = {apiVersion}\n" +
        "API Capabilities = {apiCapabilities}\n" +
        "Chip type = {chipType}\n" +
        "Chip version = {chipVersion}\n" +
        "Node IDs = {nodeIds}")]
    public static partial void LogInitData(
        this ILogger logger,
        byte apiVersion,
        GetInitDataCapabilities apiCapabilities,
        byte chipType,
        byte chipVersion,
        string nodeIds);
}

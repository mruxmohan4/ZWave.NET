using Microsoft.Extensions.Logging;
using ZWave.Serial;

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
        Message = "Received Serial API data frame with unknown type `{dataFrameType}`")]
    public static partial void LogSerialApiDataFrameUnknownType(this ILogger logger, DataFrameType dataFrameType);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Initialization sequence starting")]
    public static partial void LogInitializing(this ILogger logger);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "Performing soft reset")]
    public static partial void LogSoftReset(this ILogger logger);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Initialization sequence complete")]
    public static partial void LogInitialized(this ILogger logger);
}

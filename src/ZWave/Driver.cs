using Microsoft.Extensions.Logging;
using ZWave.Commands;
using ZWave.Serial;

namespace ZWave;

public sealed class Driver : IDisposable
{
    private record struct AwaitedCommand(CommandId CommandId, TaskCompletionSource<DataFrame> TaskCompletionSource);

    private readonly ILogger _logger;

    private readonly Stream _stream;

    private readonly ZWaveFrameListener _frameListener;

    private readonly CommandScheduler _commandScheduler;
    
    private readonly Controller _controller;

    private byte _lastSessionId = 0;

    private AwaitedCommand? _awaitedCommandResponse;

    // Currenty this is the only non-callback request we wait on. If there are more, this should become a pattern.
    private TaskCompletionSource<SerialApiStartedRequest>? _serialApiStartedTaskCompletionSource;

    private Driver(ILogger logger, Stream stream)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _frameListener = new ZWaveFrameListener(logger, stream, ProcessFrame);
        _commandScheduler = new CommandScheduler(logger, stream, this);
        _controller = new Controller(logger, this);
    }

    public static async Task<Driver> CreateAsync(
        ILogger logger,
        string portName,
        CancellationToken cancellationToken)
    {
        var stream = new ZWaveSerialPortStream(logger, portName);
        var driver = new Driver(logger, stream);
        await driver.InitializeAsync(cancellationToken).ConfigureAwait(false);

        return driver;
    }

    public void Dispose()
    {
        _frameListener.Dispose();
        _stream.Dispose();
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogDriverInitializing();

        // Perform initialization sequence (INS12350 6.1)
        await _stream.WriteAsync(Frame.NAK.Data, cancellationToken).ConfigureAwait(false);

        // TODO: Add retry logic.
        try
        {
            await SoftResetAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new ZWaveException(ZWaveErrorCode.DriverInitializationFailed, "Soft reset failed", ex);
        }

        await _controller.IdentifyAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDriverInitialized();
    }

    private async Task SoftResetAsync(CancellationToken cancellationToken)
    {
        _logger.LogSoftReset();
        var softResetRequest = SerialApiSoftResetRequest.Create();
        await _commandScheduler.SendCommandAsync(softResetRequest.Frame, cancellationToken);

        // TODO: Pause sending any new commands until we're sure everything is working again.
        _serialApiStartedTaskCompletionSource = new TaskCompletionSource<SerialApiStartedRequest>();
        try
        {
            // Wait for 1.5s per spec, unless we get an affirmative signal back that the serial API has started.
            TimeSpan serialApiStartedWaitTime = TimeSpan.FromMilliseconds(1500);

            SerialApiStartedRequest serialApiStartedRequest = await _serialApiStartedTaskCompletionSource.Task.WaitAsync(serialApiStartedWaitTime);

            // TODO: Log wakeup reason and maybe other things
            // TODO: Do something with the response
        }
        catch (TimeoutException)
        {
            // Some if we don't get the signal, assume the soft reset was successful after the wait time.
        }
        finally
        {
            _serialApiStartedTaskCompletionSource = null;
        }
    }

    private void ProcessFrame(Frame frame)
    {
        switch (frame.Type)
        {
            case FrameType.ACK:
            case FrameType.NAK:
            case FrameType.CAN:
            {
                _commandScheduler.NotifyCommandDelivered(frame.Type);
                break;
            }
            case FrameType.Data:
            {
                DataFrame dataFrame = frame.ToDataFrame();
                ProcessDataFrame(dataFrame);
                break;
            }
            default:
            {
                // Ignore anything we don't recognize.
                // TODO: Log
                break;
            }
        }
    }

    private void ProcessDataFrame(DataFrame frame)
    {
        // From INS12350 5.4.6
        // Data frame MUST be considered invalid if it is received with an invalid checksum.
        // A host or Z-Wave chip MUST return a NAK frame in response to an invalid Data frame.
        if (!frame.IsChecksumValid())
        {
            _logger.LogSerialApiInvalidDataFrame(frame);

            SendFrame(Frame.NAK);

            // From INS12350 6.4.2
            // If a host application detects an invalid checksum three times in a row when receiving data frames, the 
            // host application SHOULD invoke a hard reset of the device. If a hard reset line is not available, a soft 
            // reset indication SHOULD be issued for the device.
            // TODO
        }

        switch (frame.Type)
        {
            case DataFrameType.REQ:
            {
                // Immediately send the ACK
                SendFrame(Frame.ACK);

                if (frame.CommandId == CommandId.SerialApiStarted
                    && _serialApiStartedTaskCompletionSource != null)
                {
                    _serialApiStartedTaskCompletionSource.SetResult(SerialApiStartedRequest.Create(frame));
                }

                // TODO: Handle requests
                break;
            }
            case DataFrameType.RES:
            {
                if (_awaitedCommandResponse == null)
                {
                    // We weren't expecting a response. Just drop it
                    // TODO: Log
                    return;
                }

                if (_awaitedCommandResponse.Value.CommandId != frame.CommandId)
                {
                    // We weren't expecting this response. Just drop it
                    // TODO: Log
                    return;
                }

                _awaitedCommandResponse.Value.TaskCompletionSource.SetResult(frame);
                _awaitedCommandResponse = null;
                break;
            }
            default:
            {
                // From INS12350 5.4.3
                // A receiving end MUST ignore reserved Type values.
                _logger.LogSerialApiDataFrameUnknownType(frame.Type);
                break;
            }
        }
    }

    internal byte GetNextSessionId()
    {
        // TODO: Thread-safety?
        byte nextSessionId = _lastSessionId;

        nextSessionId++;

        // Avoid 0
        if (nextSessionId == 0)
        {
            nextSessionId++;
        }

        _lastSessionId = nextSessionId;
        return nextSessionId;
    }

    internal Task<DataFrame> WaitForResponseAsync(CommandId commandId)
    {
        var tcs = new TaskCompletionSource<DataFrame>();
        _awaitedCommandResponse = new AwaitedCommand(commandId, tcs);
        return tcs.Task;
    }

    // TODO: This is super awkward
    internal async Task<(TResponse Response, TRequest Callback)?> SendRequestCommandWithCallbackAsync<TRequest, TResponse>(
        ICommand<TRequest> request,
        CancellationToken cancellationToken)
        where TRequest : struct, ICommand<TRequest>
        where TResponse : struct, ICommand<TResponse>
    {
        TResponse response = await SendCommandAsync<TRequest, TResponse>(request, cancellationToken);

        // TODO: Validate the response, wait for callback
        TRequest callback = default;

        return (response, callback);
    }

    internal async Task<TResponse> SendCommandAsync<TRequest, TResponse>(
        ICommand<TRequest> request,
        CancellationToken cancellationToken)
        where TRequest : struct, ICommand<TRequest>
        where TResponse : struct, ICommand<TResponse>
    {
        DataFrame responseFrame = await _commandScheduler.SendCommandAsync(
            request.Frame,
            expectResponse: true,
            cancellationToken).ConfigureAwait(false);
        return TResponse.Create(responseFrame);
    }

    private void SendFrame(Frame frame)
    {
        // TODO: Make async? Frames are pretty small, so maybe not?
        // TODO: Does this need to be coordinated with CommandScheduler?
        _logger.LogSerialApiFrameSent(frame);
        _stream.Write(frame.Data.Span);
    }
}

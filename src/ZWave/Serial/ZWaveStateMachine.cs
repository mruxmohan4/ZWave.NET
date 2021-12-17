using Microsoft.Extensions.Logging;

namespace ZWave.Serial;

public sealed class ZWaveStateMachine : IDisposable
{
    private record struct AwaitedCommand(CommandId CommandId, TaskCompletionSource<DataFrame> TaskCompletionSource);

    private readonly ILogger _logger;

    private readonly Stream _stream;

    private readonly ZWaveFrameListener _listener;

    private readonly List<AwaitedCommand> _awaitedCommands = new List<AwaitedCommand>();

    private State _state;

    public ZWaveStateMachine(ILogger logger, Stream stream)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _listener = new ZWaveFrameListener(logger, stream, ProcessFrame);
        _state = State.Uninitialized;
    }

    private enum State
    {
        Uninitialized,
        Initializing,
        Idle
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_state != State.Uninitialized)
        {
            throw new InvalidOperationException("The state machine is already initialized");
        }

        _state = State.Initializing;
        _logger.LogInitializing();

        // Perform initialization sequence (INS12350 6.1)
        await _stream.WriteAsync(Frame.NAK.Data, cancellationToken).ConfigureAwait(false);

        // TODO: how do we know whether we should soft or hard reset?

        // Soft reset
        _logger.LogSoftReset();
        var softResetRequest = new DataFrame(DataFrameType.REQ, CommandId.SerialApiSoftReset, ReadOnlyMemory<byte>.Empty);
        softResetRequest.WriteToStream(_stream);

        // Wait for 1.5s per spec, unless we get an affirmative signal back that the serial API has started.
        TimeSpan serialApiStartedWaitTime = TimeSpan.FromMilliseconds(1500);
        DataFrame? frame = await WaitForCommandAsync(
            CommandId.SerialApiStarted,
            serialApiStartedWaitTime,
            cancellationToken).ConfigureAwait(false);
        if (frame == null)
        {
            // TODO: Try reconnecting the port (uh oh), and then wait for
            // SerialApiStarted again (5 sec [configureable] timeout), then check if the
            // controller responds to some arbirary command (GetControllerVersion?).
            // Retry a few times.
            // TODO: Fail in some better way
            throw new Exception();
        }

        _logger.LogInitialized();
        _state = State.Idle;
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private void ProcessFrame(Frame frame)
    {
        switch (frame.Type)
        {
            case FrameType.ACK:
            {
                // TODO
                break;
            }
            case FrameType.NAK:
            {
                // TODO
                break;
            }
            case FrameType.CAN:
            {
                // TODO
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
        if (!frame.IsChecksumValid)
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
                SendFrame(Frame.ACK);

                // TODO: What if there are more than one awaiter?
                // TODO: Thread-safety
                AwaitedCommand? matchingAwaitedCommand = null;
                foreach (AwaitedCommand awaitedCommand in _awaitedCommands)
                {
                    if (frame.CommandId == awaitedCommand.CommandId)
                    {
                        matchingAwaitedCommand = awaitedCommand;
                    }
                }

                if (matchingAwaitedCommand.HasValue)
                {
                    matchingAwaitedCommand.Value.TaskCompletionSource.SetResult(frame);
                    _awaitedCommands.Remove(matchingAwaitedCommand.Value);
                }

                // TODO
                break;
            }
            case DataFrameType.RES:
            {
                // TODO
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

        // TODO
    }

    private void SendFrame(Frame frame)
    {
        // TODO: Make async? Frames are pretty small, so maybe not?
        _logger.LogSerialApiFrameSent(frame);
        _stream.Write(frame.Data.Span);
    }

    private async Task<DataFrame?> WaitForCommandAsync(CommandId commandId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        TaskCompletionSource<DataFrame> taskCompletionSource = new TaskCompletionSource<DataFrame>();
        _awaitedCommands.Add(new AwaitedCommand(commandId, taskCompletionSource));

        try
        {
            return await taskCompletionSource.Task.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }
}

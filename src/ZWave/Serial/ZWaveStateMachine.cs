using System.IO;

using Microsoft.Extensions.Logging;

using ZWave.Commands;

namespace ZWave.Serial;

public sealed class ZWaveStateMachine : IDisposable
{
    private record struct AwaitedCommand(DataFrameType Type, CommandId CommandId, TaskCompletionSource<DataFrame> TaskCompletionSource);

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
        var softResetRequest = SerialApiSoftResetRequest.Create();
        SendCommand(softResetRequest);

        // Wait for 1.5s per spec, unless we get an affirmative signal back that the serial API has started.
        TimeSpan serialApiStartedWaitTime = TimeSpan.FromMilliseconds(1500);
        DataFrame? frame = await WaitForCommandAsync(
            DataFrameType.REQ,
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

        // TODO: Implement better request/response pattern
        var memoryGetIdRequest = MemoryGetIdRequest.Create();
        SendCommand(memoryGetIdRequest);
        DataFrame? memoryGetIdResponse = await WaitForCommandAsync(
            DataFrameType.RES,
            CommandId.MemoryGetId,
            TimeSpan.FromSeconds(5), // TODO: This is arbitrary. Check the spec
            cancellationToken).ConfigureAwait(false);
        if (memoryGetIdResponse == null)
        {
            // TODO: Fail in some better way
            throw new Exception();
        }

        // TODO: Do something with the controller id response

        // TODO: Implement better request/response pattern
        var getSerialCapabilitiesRequest = GetSerialApiCapabilitiesRequest.Create();
        SendCommand(getSerialCapabilitiesRequest);
        DataFrame? getSerialCapabilitiesResponse = await WaitForCommandAsync(
            DataFrameType.RES,
            CommandId.GetSerialApiCapabilities,
            TimeSpan.FromSeconds(5), // TODO: This is arbitrary. Check the spec
            cancellationToken).ConfigureAwait(false);
        if (getSerialCapabilitiesResponse == null)
        {
            // TODO: Fail in some better way
            throw new Exception();
        }

        // TODO: Do something with the serial api capabilities

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

        // TODO: What if there are more than one awaiter?
        // TODO: Thread-safety
        AwaitedCommand? matchingAwaitedCommand = null;
        foreach (AwaitedCommand awaitedCommand in _awaitedCommands)
        {
            if (frame.Type == awaitedCommand.Type
                && frame.CommandId == awaitedCommand.CommandId)
            {
                matchingAwaitedCommand = awaitedCommand;
            }
        }

        if (matchingAwaitedCommand.HasValue)
        {
            matchingAwaitedCommand.Value.TaskCompletionSource.SetResult(frame);
            _awaitedCommands.Remove(matchingAwaitedCommand.Value);
        }

        switch (frame.Type)
        {
            case DataFrameType.REQ:
            {
                SendFrame(Frame.ACK);

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

    private void SendFrame(DataFrame frame)
    {
        _logger.LogSerialApiDataFrameSent(frame);
        _stream.Write(frame.Data.Span);
    }

    private void SendCommand(ICommand command)
        => SendFrame(command.Frame);

    private async Task<DataFrame?> WaitForCommandAsync(DataFrameType type, CommandId commandId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        TaskCompletionSource<DataFrame> taskCompletionSource = new TaskCompletionSource<DataFrame>();
        _awaitedCommands.Add(new AwaitedCommand(type, commandId, taskCompletionSource));

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

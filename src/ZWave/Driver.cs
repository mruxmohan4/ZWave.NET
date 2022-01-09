using Microsoft.Extensions.Logging;
using ZWave.CommandClasses;
using ZWave.Serial;
using ZWave.Serial.Commands;

namespace ZWave;

public sealed class Driver : IDisposable
{
    private record struct AwaitedCommand(CommandId CommandId, TaskCompletionSource<DataFrame> TaskCompletionSource);

    private record struct UnresolvedCallbackKey(CommandId CommandId, byte SessionId);

    private readonly ILogger _logger;

    private readonly ZWaveSerialPortStream _stream;

    private readonly ZWaveFrameListener _frameListener;

    private readonly CommandScheduler _commandScheduler;

    // Lock anything related to session ids or callbacks
    private readonly object _callbackLock = new object();

    private readonly Dictionary<UnresolvedCallbackKey, TaskCompletionSource<DataFrame>> _unresolvedCallbacks = new Dictionary<UnresolvedCallbackKey, TaskCompletionSource<DataFrame>>();

    private byte _lastSessionId = 0;

    // Note that only one request -> response session can happen at a time.
    private AwaitedCommand? _awaitedCommandResponse;

    // Currenty this is the only non-callback request we wait on. If there are more, this should become a pattern.
    private TaskCompletionSource<SerialApiStartedRequest>? _serialApiStartedTaskCompletionSource;

    private Driver(ILogger logger, ZWaveSerialPortStream stream)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _frameListener = new ZWaveFrameListener(logger, stream, ProcessFrame);
        _commandScheduler = new CommandScheduler(logger, stream, this);
        Controller = new Controller(logger, this);
    }

    public Controller Controller { get; }

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

        await Controller.IdentifyAsync(cancellationToken).ConfigureAwait(false);

        // Begin interviewing the nodes, starting with and waiting for the controller node
        Node controllerNode = Controller.Nodes[Controller.NodeId];
        await controllerNode.InterviewAsync(cancellationToken).ConfigureAwait(false);
        foreach (KeyValuePair<byte, Node> pair in Controller.Nodes)
        {
            Node node = pair.Value;
            if (node != controllerNode)
            {
                // This is intentionally fire-and-forget
                _ = node.InterviewAsync(cancellationToken);
            }
        }

        _logger.LogDriverInitialized();
    }

    public async Task SoftResetAsync(CancellationToken cancellationToken)
    {
        _logger.LogSoftReset();
        var softResetRequest = SoftResetRequest.Create();
        await SendCommandAsync(softResetRequest, cancellationToken)
            .ConfigureAwait(false);

        // TODO: Pause sending any new commands until we're sure everything is working again.
        _serialApiStartedTaskCompletionSource = new TaskCompletionSource<SerialApiStartedRequest>();
        try
        {
            // Wait for 1.5s per spec, unless we get an affirmative signal back that the serial API has started.
            TimeSpan serialApiStartedWaitTime = TimeSpan.FromMilliseconds(1500);

            // TODO: this causes exceptions in the listener. The Serial Port management needs to be done better.
            SerialApiStartedRequest serialApiStartedRequest = await _serialApiStartedTaskCompletionSource.Task
                .WaitAsync(serialApiStartedWaitTime, cancellationToken)
                .ConfigureAwait(false);

            // TODO: Log wakeup reason and maybe other things
            // TODO: Do something with the response
            return;
        }
        catch (TimeoutException)
        {
            // If we don't get the signal, assume the soft reset was successful after the wait time.

            // Some controllers disconnect after a soft reset. If that's the case, re-open.
            if (!_stream.IsOpen)
            {
                _stream.Open();
            }
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

                // Try handling unsolicited requests first, then check if it's a callback we're waiting for.
                switch (frame.CommandId)
                {
                    case CommandId.SerialApiStarted:
                    {
                        // Unblock the task. TODO: Log if null (we got it unexpectedly)
                        if (_serialApiStartedTaskCompletionSource != null)
                        {
                            _serialApiStartedTaskCompletionSource.SetResult(SerialApiStartedRequest.Create(frame));
                        }

                        break;
                    }
                    case CommandId.ApplicationUpdate:
                    {
                        var applicationUpdateRequest = ApplicationUpdateRequest.Create(frame);
                        if (applicationUpdateRequest.Event == ApplicationUpdateEvent.NodeInfoReceived)
                        {
                            var node = Controller.Nodes[applicationUpdateRequest.Generic.NodeId];
                            node.NotifyNodeInfoReceived(applicationUpdateRequest);
                        }

                        break;
                    }
                    case CommandId.ApplicationCommandHandler:
                    {
                        var applicationCommandHandler = ApplicationCommandHandler.Create(frame);
                        var node = Controller.Nodes[applicationCommandHandler.NodeId];
                        var commandClassFrame = new CommandClassFrame(applicationCommandHandler.Payload);
                        node.ProcessCommand(commandClassFrame);
                        break;
                    }
                    default:
                    {
                        // This assumes the first command parameter is always the session id. If this is ever
                        // found not to be the case, we'll need to add the callback index to the key.
                        var callbackKey = new UnresolvedCallbackKey(frame.CommandId, frame.CommandParameters.Span[0]);
                        lock (_callbackLock)
                        {
                            if (_unresolvedCallbacks.TryGetValue(callbackKey, out TaskCompletionSource<DataFrame>? tcs))
                            {
                                tcs.SetResult(frame);
                                _unresolvedCallbacks.Remove(callbackKey);
                                return;
                            }
                        }

                        break;
                    }
                }

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
        lock (_callbackLock)
        {
            byte nextSessionId = _lastSessionId;

            bool IsSessionIdInUse(byte sessionId)
            {
                foreach (KeyValuePair<UnresolvedCallbackKey, TaskCompletionSource<DataFrame>> pair in _unresolvedCallbacks)
                {
                    if (pair.Key.SessionId == sessionId)
                    {
                        return true;
                    }
                }

                return false;
            }

            // Avoid 0 which indicates disablement of the callback functionality, as well as sessions currently in use.
            do
            {
                nextSessionId++;
            }
            while (nextSessionId == 0 || !IsSessionIdInUse(nextSessionId));

            _lastSessionId = nextSessionId;
            return nextSessionId;
        }
    }

    internal Task<DataFrame> WaitForResponseAsync(CommandId commandId)
    {
        var tcs = new TaskCompletionSource<DataFrame>();
        _awaitedCommandResponse = new AwaitedCommand(commandId, tcs);
        return tcs.Task;
    }

    // TODO: This name is terrible. Consider doing more type magic for req/res vs callback flow
    internal async Task<TCallback> SendCommandExpectingCallbackAsync<TRequest, TCallback>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : struct, IRequestWithCallback<TRequest>
        where TCallback : struct, ICommand<TCallback>
    {
        byte sessionId = request.SessionId;

        if (TRequest.ExpectsResponseStatus)
        {
            ResponseStatusResponse response = await SendCommandAsync<TRequest, ResponseStatusResponse>(request, cancellationToken)
                .ConfigureAwait(false);
            if (!response.WasRequestAccepted)
            {
                throw new ZWaveException(ZWaveErrorCode.CommandFailed, "Response status indicated failure");
            }
        }
        else
        {
            await SendCommandAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        TaskCompletionSource<DataFrame> tcs = new TaskCompletionSource<DataFrame>();
        var callbackKey = new UnresolvedCallbackKey(TCallback.CommandId, sessionId);
        lock (_callbackLock)
        {
            _unresolvedCallbacks.Add(callbackKey, tcs);
        }

        DataFrame callbackFrame = await tcs.Task;
        return TCallback.Create(callbackFrame);
    }

    internal async Task<TResponse> SendCommandAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : struct, ICommand<TRequest>
        where TResponse : struct, ICommand<TResponse>
    {
        DataFrame responseFrame = await _commandScheduler.SendCommandAsync(
            request.Frame,
            expectResponse: true)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return TResponse.Create(responseFrame);
    }

    internal async Task SendCommandAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : struct, ICommand<TRequest>
    {
        await _commandScheduler.SendCommandAsync(request.Frame)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task SendCommandAsync<TCommand>(
        TCommand request,
        byte nodeId,
        CancellationToken cancellationToken)
        where TCommand : struct, ICommand
    {
        const TransmissionOptions transmissionOptions = TransmissionOptions.ACK | TransmissionOptions.AutoRoute | TransmissionOptions.Explore;
        byte sessionId = GetNextSessionId();
        SendDataRequest sendDataRequest = SendDataRequest.Create(nodeId, request.Frame.Data.Span, transmissionOptions, sessionId);
        ResponseStatusResponse response = await SendCommandAsync<SendDataRequest, ResponseStatusResponse>(sendDataRequest, cancellationToken)
            .ConfigureAwait(false);
        if (!response.WasRequestAccepted)
        {
            throw new ZWaveException(ZWaveErrorCode.CommandFailed, "Response status indicated failure");
        }

        TaskCompletionSource<DataFrame> tcs = new TaskCompletionSource<DataFrame>();
        var callbackKey = new UnresolvedCallbackKey(CommandId.SendData, sessionId);
        lock (_callbackLock)
        {
            _unresolvedCallbacks.Add(callbackKey, tcs);
        }

        // Intentionally not awaiting this task. The callback only contains a transmit report,
        // which the caller doesn't care about.
        _ = tcs.Task.ContinueWith(task =>
        {
            // The unresolved callback tasks currently never get cancelled or fault. If this changes,
            // consider what to do here in those cases.
            if (task.IsCompletedSuccessfully)
            {
                DataFrame callbackFrame = task.Result;
                SendDataCallback callback = SendDataCallback.Create(callbackFrame);

                // TODO: Consume the transmit report.
            }
        });
    }

    private void SendFrame(Frame frame)
    {
        // TODO: Make async? Frames are pretty small, so maybe not?
        // TODO: Does this need to be coordinated with CommandScheduler?
        _logger.LogSerialApiFrameSent(frame);
        _stream.Write(frame.Data.Span);
    }
}

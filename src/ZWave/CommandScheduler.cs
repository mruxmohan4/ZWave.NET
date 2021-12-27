using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZWave.Serial;

namespace ZWave;

internal sealed class CommandScheduler : IDisposable
{
    private record struct Session(
        DataFrame Request,
        bool ExpectResponse,
        TaskCompletionSource<DataFrame> SessionCompletion);

    // INS12350 6.3 specifies that the host should use 3 retransmissions, meaning 4 total attempts
    private const int MaxTransmissionAttempts = 4;

    // INS12350 6.2.2
    private static readonly TimeSpan FrameDeliveryTimeout = TimeSpan.FromMilliseconds(1600);

    private readonly ILogger _logger;

    private readonly Stream _stream;

    private readonly Driver _driver;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly AsyncAutoResetEvent _newSessionEvent = new AsyncAutoResetEvent();

    private readonly ConcurrentQueue<Session> _queue = new ConcurrentQueue<Session>();

    private TaskCompletionSource<FrameType>? _commandDelivered;

    public CommandScheduler(
        ILogger logger,
        Stream stream,
        Driver driver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _cancellationTokenSource = new CancellationTokenSource();

        // Note: Since we're starting out own task, we don't need to ConfigureAwait anywhere dowstream.
        Task.Run(() => SendLoop());
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _newSessionEvent.Dispose();
    }

    public Task SendCommandAsync(DataFrame request)
        => SendCommandAsync(request, expectResponse: false);

    public Task<DataFrame> SendCommandAsync(DataFrame request, bool expectResponse)
    {
        var sessionCompletion = new TaskCompletionSource<DataFrame>();
        var session = new Session(
            request,
            expectResponse,
            sessionCompletion);

        _queue.Enqueue(session);
        _newSessionEvent.Set();

        return sessionCompletion.Task;
    }

    public void NotifyCommandDelivered(FrameType frameType)
    {
        if (frameType == FrameType.Data)
        {
            throw new ArgumentException("Data frames do not correspond to command reception", nameof(frameType));
        }

        if (_commandDelivered == null)
        {
            // We received a frame delivery notification unexpectedly. Just ignore.
            // TODO: Log
            return;
        }

        _commandDelivered.SetResult(frameType);
    }

    private async Task SendLoop()
    {
        try
        {
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                // Try to start processing the next session.
                if (!_queue.TryDequeue(out Session session))
                {
                    // If there was nothing in the queue, wait until there is a new session.
                    await _newSessionEvent.WaitAsync();
                    continue;
                }

                bool commandSent = await TrySendCommandAsync(session.Request, cancellationToken);
                if (!commandSent)
                {
                    // TODO: Should soft reset. Or maybe let the caller decide?
                    session.SessionCompletion.SetException(new ZWaveException(ZWaveErrorCode.CommandSendFailed, "Command failed to send"));
                    continue;
                }

                // If this session only required a request without any response or callback
                if (!session.ExpectResponse)
                {
                    session.SessionCompletion.SetResult(default);
                    continue;
                }

                // TODO: Should there be a timeout?
                DataFrame response = await _driver.WaitForResponseAsync(session.Request.CommandId);

                session.SessionCompletion.SetResult(response);
            }
        }
        catch (OperationCanceledException)
        {
            // If cancelled, gracefully stop
        }
    }

    private async Task<bool> TrySendCommandAsync(
        DataFrame dataFrame,
        CancellationToken cancellationToken)
    {
        for (int transmissionAttempt = 0; transmissionAttempt < MaxTransmissionAttempts; transmissionAttempt++)
        {
            // INS12350 6.3 specifies a wait time for retransmissions
            if (transmissionAttempt > 0)
            {
                int waitTimeMillis = 100 + ((transmissionAttempt - 1) * 1000);
                await Task.Delay(waitTimeMillis, cancellationToken);
            }

            _commandDelivered = new TaskCompletionSource<FrameType>();

            // Send the command
            await _stream.WriteAsync(dataFrame.Data, cancellationToken);
            _logger.LogSerialApiDataFrameSent(dataFrame);

            // Wait for delivery confirmation
            FrameType commandRecievedType;
            try
            {
                commandRecievedType = await _commandDelivered.Task.WaitAsync(FrameDeliveryTimeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                // INS12350 6.2.2 specifies that timeouts are treates as a NAK
                commandRecievedType = FrameType.NAK;
            }
            finally
            {
                _commandDelivered = null;
            }

            if (commandRecievedType == FrameType.ACK)
            {
                return true;
            }

            // TODO: Log
        }

        // Exhausted all retries
        return false;
    }
}

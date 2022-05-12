using System.Buffers;
using System.IO.Pipelines;

using Microsoft.Extensions.Logging;

namespace ZWave.Serial;

internal sealed partial class ZWaveFrameListener : IDisposable
{
    private readonly PipeReader _reader;

    private readonly CancellationTokenSource _cancellationTokenSource;

    public ZWaveFrameListener(
        ILogger logger,
        Stream stream,
        Action<Frame> frameHandler)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (frameHandler == null)
        {
            throw new ArgumentNullException(nameof(frameHandler));
        }

        _reader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
        _cancellationTokenSource = new CancellationTokenSource();

        // Note: Since we're starting out own task, we don't need to ConfigureAwait anywhere dowstream.
        Task.Run(() => ReadAsync(logger, _reader, frameHandler, _cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _reader.CancelPendingRead();
    }

    private static async Task ReadAsync(
        ILogger logger,
        PipeReader reader,
        Action<Frame> frameHandler,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                ReadResult readResult = await reader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                if (readResult.IsCanceled)
                {
                    break;
                }

                while (FrameParser.TryParseData(logger, ref buffer, out Frame frame))
                {
                    // TODO: Should we be invoking user code directly like this?
                    // It blocks parsing new frames. How should exceptions be handled?
                    // Maybe a better approach would be to use a Pipeline the caller can consume?
                    frameHandler(frame);
                }

                // Tell the PipeReader how much of the buffer has been consumed.
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming.
                if (readResult.IsCompleted)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                // Swallow. If a specific read is cancelled, just keep retrying.
                // TODO: Log
            }
        }
    }
}

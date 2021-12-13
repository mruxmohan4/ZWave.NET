using System.Buffers;
using System.IO.Pipelines;

namespace ZWave;

public sealed class ZWaveFrameListener : IDisposable
{
    private readonly Stream _stream;

    private readonly PipeReader _reader;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private bool _disposed = false;

    public ZWaveFrameListener(Stream stream, Action<Frame> frameHandler)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        if (frameHandler == null)
        {
            throw new ArgumentNullException(nameof(frameHandler));
        }

        _reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ReadAsync(_reader, frameHandler, _cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _reader.CancelPendingRead();

        _disposed = true;
    }

    private static async Task ReadAsync(
        PipeReader reader,
        Action<Frame> frameHandler,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ReadResult readResult = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            ReadOnlySequence<byte> buffer = readResult.Buffer;

            if (readResult.IsCanceled)
            {
                break;
            }

            while (FrameParser.TryParseData(ref buffer, out Frame frame))
            {
                // TODO: Should we be invoking user code directly like this?
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
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ZWaveFrameListener));
        }
    }
}

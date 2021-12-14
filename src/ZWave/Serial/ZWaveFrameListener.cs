using System.Buffers;
using System.IO;
using System.IO.Pipelines;

namespace ZWave.Serial;

public sealed class ZWaveFrameListener : IDisposable
{
    private readonly PipeReader _reader;

    private readonly CancellationTokenSource _cancellationTokenSource;

    public ZWaveFrameListener(Stream stream, Action<Frame> frameHandler)
    {
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
        Task.Run(() => ReadAsync(_reader, frameHandler, _cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _reader.CancelPendingRead();
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
    }
}

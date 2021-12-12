using System.Buffers;
using System.IO.Pipelines;
using System.Threading;

namespace ZWave;

public sealed class ZWaveSerialPort : IDisposable
{
    private readonly ZWaveSerialPortStream _stream;

    private readonly PipeReader _reader;

    private readonly Task _readTask;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private bool _disposed = false;

    public ZWaveSerialPort(string portName)
    {
        _stream = new ZWaveSerialPortStream(portName);
        _reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));
        _cancellationTokenSource = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadAsync(_reader, _cancellationTokenSource.Token));
    }

    public void WriteAsync(byte[] buffer)
    {
        CheckDisposed();

        _stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _reader.CancelPendingRead();
        _stream.Dispose();

        _disposed = true;
    }

    private static async Task ReadAsync(
        PipeReader reader,
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

            // TODO: Parse buffer
            ////_reader.AdvanceTo(buffer.Start, buffer.End);

            if (readResult.IsCompleted)
            {
                if (!buffer.IsEmpty)
                {
                    throw new InvalidDataException("Incomplete message.");
                }

                break;
            }
        }
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ZWaveSerialPort));
        }
    }
}

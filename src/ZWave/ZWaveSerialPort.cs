using System.Buffers;
using System.IO.Pipelines;
using System.IO.Ports;

namespace ZWave;

public sealed class ZWaveSerialPort : IDisposable
{
    private readonly SerialPort _port;

    private PipeReader? _reader;

    private Task? _readTask;

    private CancellationTokenSource? _cancellationTokenSource;

    private bool _disposed = false;

    public ZWaveSerialPort(string portName)
    {
        _port = new SerialPort(
            portName,
            baudRate: 115200,
            parity: Parity.None,
            dataBits: 8,
            stopBits: StopBits.One);
    }

    public bool IsConnected => _port.IsOpen;

    public void Connect()
    {
        CheckDisposed();

        if (_port.IsOpen)
        {
            throw new InvalidOperationException("The port is already connected");
        }

        _port.Open();
        _port.DiscardInBuffer();

        _reader = PipeReader.Create(_port.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));
        _cancellationTokenSource = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadAsync(_reader, _cancellationTokenSource.Token));
    }

    public void Disconnect()
    {
        CheckDisposed();

        if (!_port.IsOpen)
        {
            throw new InvalidOperationException("The port is not connected");
        }

        DisconnectInternal();
    }

    public void WriteAsync(byte[] buffer)
    {
        CheckDisposed();

        if (!_port.IsOpen)
        {
            throw new InvalidOperationException("The port is not connected");
        }

        _port.BaseStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DisconnectInternal();

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

    private void DisconnectInternal()
    {
        _cancellationTokenSource?.Cancel();
        _reader?.CancelPendingRead();
        _port.Close();
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ZWaveSerialPort));
        }
    }
}

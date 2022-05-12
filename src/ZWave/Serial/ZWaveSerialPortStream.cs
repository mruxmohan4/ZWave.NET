using System.IO.Ports;

using Microsoft.Extensions.Logging;

namespace ZWave.Serial;

/// <summary>
/// Provides a stream for the Z-Wave Serial port.
/// </summary>
/// <remarks>
/// This simply wraps <see cref="SerialPort.BaseStream"/> while handling lifetime management of the underlying <see cref="SerialPort"/>.
/// </remarks>
public sealed class ZWaveSerialPortStream : Stream
{
    // INS12350 defines a Z-Wave module as unresponsive after 4 seconds, so retry 4 times with a 1 second delay between each.
    private const int MaxConnectionRetries = 4;
    private const int ConnectionDelay = 1000;

    private readonly ILogger _logger;

    private readonly SerialPort _port;

    public ZWaveSerialPortStream(ILogger logger, string portName)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrEmpty(portName))
        {
            throw new ArgumentNullException(nameof(portName));
        }

        _port = new SerialPort(
            portName,
            baudRate: 115200,
            parity: Parity.None,
            dataBits: 8,
            stopBits: StopBits.One);
        EnsurePortOpened(isReopen: false);
    }

    private void EnsurePortOpened(bool isReopen)
    {
        if (_port.IsOpen)
        {
            return;
        }

        int retryCount = 0;
        while (true)
        {
            try
            {
                _port.Open();
                break;
            }
            catch (InvalidOperationException)
            {
                // Another thread may have reopened the port already. If so, just bail
                if (_port.IsOpen)
                {
                    return;
                }
                else
                {
                    throw;
                }
            }
            catch (FileNotFoundException)
            {
                // If the port goes away momentarily, for example during a soft reset, retry opening the port a few times
                if (isReopen && retryCount <= MaxConnectionRetries)
                {
                    retryCount++;
                    Thread.Sleep(ConnectionDelay);
                }
                else
                {
                    throw;
                }
            }
        }

        _port.DiscardInBuffer();
        _port.DiscardOutBuffer();

        if (isReopen)
        {
            _logger.LogSerialApiPortReopened(_port.PortName);
        }
        else
        {
            _logger.LogSerialApiPortOpened(_port.PortName);
        }
    }

    private void PerformWithRetries(Action action)
    {
        EnsurePortOpened(isReopen: true);
        try
        {
            action();
        }
        catch (IOException)
        {
            EnsurePortOpened(isReopen: true);
            action();
        }
    }

    private T PerformWithRetries<T>(Func<T> func)
    {
        EnsurePortOpened(isReopen: true);
        try
        {
            return func();
        }
        catch (IOException)
        {
            EnsurePortOpened(isReopen: true);
            return func();
        }
    }

    /*
     * Actually overridden members
     */

    public override void Close()
    {
        _port.Close();
        _logger.LogSerialApiPortClosed(_port.PortName);

        base.Close();
    }

    protected override void Dispose(bool disposing)
    {
        _port.Dispose();
        base.Dispose(disposing);
    }

    /*
     * Below here are "passthrough" implementations of all abstract and virtual members to the serial port's base stream
     */

    public override bool CanRead => PerformWithRetries(() => _port.BaseStream.CanRead);

    public override bool CanSeek => PerformWithRetries(() => _port.BaseStream.CanSeek);

    public override bool CanWrite => PerformWithRetries(() => _port.BaseStream.CanWrite);

    public override bool CanTimeout => PerformWithRetries(() => _port.BaseStream.CanTimeout);

    public override long Length => PerformWithRetries(() => _port.BaseStream.Length);

    public override long Position
    {
        get => PerformWithRetries(() => _port.BaseStream.Position);
        set => PerformWithRetries(() => _port.BaseStream.Position = value);
    }

    public override int ReadTimeout
    {
        get => PerformWithRetries(() => _port.BaseStream.ReadTimeout);
        set => PerformWithRetries(() => _port.BaseStream.ReadTimeout = value);
    }

    public override int WriteTimeout
    {
        get => PerformWithRetries(() => _port.BaseStream.WriteTimeout);
        set => PerformWithRetries(() => _port.BaseStream.WriteTimeout = value);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => PerformWithRetries(() => _port.BaseStream.BeginRead(buffer, offset, count, callback, state));

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => PerformWithRetries(() => _port.BaseStream.BeginWrite(buffer, offset, count, callback, state));

    public override void CopyTo(Stream destination, int bufferSize)
        => PerformWithRetries(() => _port.BaseStream.CopyTo(destination, bufferSize));

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        => PerformWithRetries(() => _port.BaseStream.CopyToAsync(destination, bufferSize, cancellationToken));

    public override int EndRead(IAsyncResult asyncResult) => PerformWithRetries(() => _port.BaseStream.EndRead(asyncResult));

    public override void EndWrite(IAsyncResult asyncResult) => PerformWithRetries(() => _port.BaseStream.EndWrite(asyncResult));

    public override void Flush() => PerformWithRetries(() => _port.BaseStream.Flush());

    public override Task FlushAsync(CancellationToken cancellationToken)
        => PerformWithRetries(() => _port.BaseStream.FlushAsync(cancellationToken));

    public override int Read(byte[] buffer, int offset, int count) => PerformWithRetries(() => _port.BaseStream.Read(buffer, offset, count));

    public override int Read(Span<byte> buffer)
    {
        EnsurePortOpened(isReopen: true);
        try
        {
            return _port.BaseStream.Read(buffer);
        }
        catch (IOException)
        {
            EnsurePortOpened(isReopen: true);
            return _port.BaseStream.Read(buffer);
        }
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => PerformWithRetries(() => _port.BaseStream.ReadAsync(buffer, offset, count, cancellationToken));

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => PerformWithRetries(() => _port.BaseStream.ReadAsync(buffer, cancellationToken));

    public override int ReadByte() => PerformWithRetries(() => _port.BaseStream.ReadByte());

    public override long Seek(long offset, SeekOrigin origin) => PerformWithRetries(() => _port.BaseStream.Seek(offset, origin));

    public override void SetLength(long value) => PerformWithRetries(() => _port.BaseStream.SetLength(value));

    public override void Write(byte[] buffer, int offset, int count) => PerformWithRetries(() => _port.BaseStream.Write(buffer, offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsurePortOpened(isReopen: true);
        try
        {
            _port.BaseStream.Write(buffer);
        }
        catch (IOException)
        {
            EnsurePortOpened(isReopen: true);
            _port.BaseStream.Write(buffer);
        }
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => PerformWithRetries(() => _port.BaseStream.WriteAsync(buffer, offset, count, cancellationToken));

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => PerformWithRetries(() => _port.BaseStream.WriteAsync(buffer, cancellationToken));

    public override void WriteByte(byte value) => PerformWithRetries(() => _port.BaseStream.WriteByte(value));
}

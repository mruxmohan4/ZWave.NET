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
        Open();
    }

    public bool IsOpen => _port.IsOpen;

    public void Open()
    {
        _port.Open();
        _port.DiscardInBuffer();
        _port.DiscardOutBuffer();

        _logger.LogSerialApiPortOpened(_port.PortName);
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

    public override bool CanRead => _port.BaseStream.CanRead;

    public override bool CanSeek => _port.BaseStream.CanSeek;

    public override bool CanWrite => _port.BaseStream.CanWrite;

    public override bool CanTimeout => _port.BaseStream.CanTimeout;

    public override long Length => _port.BaseStream.Length;

    public override long Position
    {
        get => _port.BaseStream.Position;
        set => _port.BaseStream.Position = value;
    }

    public override int ReadTimeout
    {
        get => _port.BaseStream.ReadTimeout;
        set => _port.BaseStream.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => _port.BaseStream.WriteTimeout;
        set => _port.BaseStream.WriteTimeout = value;
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => _port.BaseStream.BeginRead(buffer, offset, count, callback, state);

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => _port.BaseStream.BeginWrite(buffer, offset, count, callback, state);

    public override void CopyTo(Stream destination, int bufferSize)
        => _port.BaseStream.CopyTo(destination, bufferSize);

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        => _port.BaseStream.CopyToAsync(destination, bufferSize, cancellationToken);

    public override int EndRead(IAsyncResult asyncResult) => _port.BaseStream.EndRead(asyncResult);

    public override void EndWrite(IAsyncResult asyncResult) => _port.BaseStream.EndWrite(asyncResult);

    public override void Flush() => _port.BaseStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => _port.BaseStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => _port.BaseStream.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => _port.BaseStream.Read(buffer);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _port.BaseStream.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _port.BaseStream.ReadAsync(buffer, cancellationToken);

    public override int ReadByte() => _port.BaseStream.ReadByte();

    public override long Seek(long offset, SeekOrigin origin) => _port.BaseStream.Seek(offset, origin);

    public override void SetLength(long value) => _port.BaseStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) => _port.BaseStream.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer) => _port.BaseStream.Write(buffer);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _port.BaseStream.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => _port.BaseStream.WriteAsync(buffer, cancellationToken);

    public override void WriteByte(byte value) => _port.BaseStream.WriteByte(value);
}

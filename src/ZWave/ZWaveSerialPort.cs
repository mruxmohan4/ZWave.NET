using System.IO.Ports;

namespace ZWave;

public sealed class ZWaveSerialPort : IDisposable
{
    private readonly SerialPort _port;

    private Thread? _readThread;

    private bool _continueReading;

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

        lock (_port)
        {
            if (_port.IsOpen)
            {
                throw new InvalidOperationException("The port is already connected");
            }

            _port.Open();
            _port.DiscardInBuffer();
            _continueReading = true;
            _readThread = new Thread(Read);
            _readThread.Start();
        }
    }

    public void Disconnect()
    {
        CheckDisposed();

        lock (_port)
        {
            if (!_port.IsOpen)
            {
                throw new InvalidOperationException("The port is not connected");
            }

            DisconnectInternal();
        }
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

    private void Read()
    {
        while (_continueReading)
        {
            try
            {
                // TODO: Process the byte
                _ = _port.ReadByte();
            }
            catch (TimeoutException) { }
        }
    }

    private void DisconnectInternal()
    {
        _continueReading = false;
        _readThread?.Join();
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

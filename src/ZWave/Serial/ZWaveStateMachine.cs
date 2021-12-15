namespace ZWave.Serial;

public sealed class ZWaveStateMachine : IDisposable
{
    private readonly Stream _stream;

    private readonly ZWaveFrameListener _listener;

    public ZWaveStateMachine(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _listener = new ZWaveFrameListener(stream, ProcessFrame);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private void ProcessFrame(Frame frame)
    {
        switch (frame.Type)
        {
            case FrameType.ACK:
            {
                // TODO
                break;
            }
            case FrameType.NAK:
            {
                // TODO
                break;
            }
            case FrameType.CAN:
            {
                // TODO
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
        if (!frame.IsChecksumValid)
        {
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
                SendFrame(Frame.ACK);

                // TODO
                break;
            }
            case DataFrameType.RES:
            {
                // TODO
                break;
            }
            default:
            {
                // From INS12350 5.4.3
                // A receiving end MUST ignore reserved Type values.
                // TODO: Log
                break;
            }
        }

        // TODO
    }

    private void SendFrame(Frame frame)
    {
        // TODO: Make async
        _stream.Write(frame.Data.Span);
    }
}

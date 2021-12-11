using System.Buffers;

namespace ZWave;

public sealed class ZWaveSerialApiParser
{
    public void TryParseData(ref ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsEmpty)
        {
            // No data?
            return;
        }

        var reader = new SequenceReader<byte>(buffer);

        if (!reader.TryRead(out byte frameHeader))
        {
            // No data?
            return;
        }

        switch (frameHeader)
        {
            case FrameHeader.ACK:
            {
                // TODO:
                break;
            }
            case FrameHeader.NAK:
            {
                // TODO:
                break;
            }
            case FrameHeader.CAN:
            {
                // TODO:
                break;
            }
            case FrameHeader.SOF:
            {
                // TODO:
                break;
            }
            default:
            {
                TryHandleDataFrame(reader);
                break;
            }
        }
    }

    private void TryHandleDataFrame(SequenceReader<byte> reader)
    {
        // We've alread read the SOF

        // TODO
    }
}

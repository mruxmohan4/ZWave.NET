using System.Buffers;

namespace ZWave;

internal static class FrameParser
{
    /// <summary>
    /// Tries to parse a ZWave frame from the provided byte sequence.
    /// </summary>
    /// <param name="sequence">The byte sequence for which to parse a frame</param>
    /// <returns>
    /// True if a frame was successfully read. If false, the caller should
    /// disregard any change of position in the buffer.
    /// </returns>
    public static bool TryParseData(ref ReadOnlySequence<byte> sequence, out Frame frame)
    {
        frame = default;

        if (sequence.IsEmpty)
        {
            // No data
            return false;
        }

        // Make a copy to avoid altering the original unless we're successful.
        ReadOnlySequence<byte> sequenceCopy = sequence;

        var reader = new SequenceReader<byte>(sequenceCopy);

        // Skip any invalid data
        if (!reader.TryReadToAny(out sequenceCopy, FrameHeader.ValidHeaders, advancePastDelimiter: false))
        {
            // We didn't find any valid data, so consume the entire sequence
            sequence = sequence.Slice(sequence.End);
            return false;
        }

        if (!reader.TryPeek(out byte frameHeader))
        {
            // Considering the call above was successful, this should never happen
            sequence = sequence.Slice(sequence.End);
            return false;
        }

        bool success;
        switch (frameHeader)
        {
            case FrameHeader.ACK:
            {
                frame = Frame.ACK;
                success = true;
                break;
            }
            case FrameHeader.NAK:
            {
                frame = Frame.NAK;
                success = true;
                break;
            }
            case FrameHeader.CAN:
            {
                frame = Frame.CAN;
                success = true;
                break;
            }
            case FrameHeader.SOF:
            {
                success = TryParseDataFrame(ref reader, out frame);
                break;
            }
            default:
            {
                // TODO
                return true;
            }
        }

        // Update the sequence for the caller
        if (success)
        {
            sequence = sequenceCopy;
        }

        return success;
    }

    private static bool TryParseDataFrame(ref SequenceReader<byte> reader, out Frame frame)
    {
        // We've alread read the SOF
        // TODO: Nope, we actually just peeked it.
        // TODO: peek the length and read the whole message into an array at once. Can we check the length?

        if (!reader.TryRead(out byte lengthByte)
            || !reader.TryRead(out byte messageType)
            || !reader.TryRead(out byte commandId))
        {
            return false;
        }

        int length = (int)lengthByte;

        // Read the command parameters. There are 3 less params to account for the length byte, message type, and command id.
        byte[] commandParameters = new byte[length - 3];
        if (!reader.TryCopyTo(commandParameters))
        {
            return false;
        }

        // Validate checksum. Note that this does not count towards the length.
        if (!reader.TryRead(out byte checksum))
        {
            return false;
        }

        // Validate checksum
        byte expectedChecksum = 0xFF;
        expectedChecksum ^= lengthByte;
        expectedChecksum ^= messageType;
        expectedChecksum ^= commandId;
        for (int i = 0; i < commandParameters.Length; i++)
        {
            expectedChecksum ^= commandParameters[i];
        }

        if (checksum != expectedChecksum)
        {
            // TODO: Handle invalid checksum. Maybe move validation to the message handler
        }

        // TODO: Raise event of complete data frame
        var dataFrame = new DataFrame(messageType, commandId, commandParameters);

        return true;
    }
}

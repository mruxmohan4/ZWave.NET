namespace ZWave;

public readonly struct Frame : IEquatable<Frame>
{
    private readonly byte[] _data;

    public Frame(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Length == 0)
        {
            throw new ArgumentException("Frame data must not be empty", nameof(data));
        }

        // Reuse the singletons for single-byte frames to allow the provided byte array to be GC'd.
        switch (data[0])
        {
            case FrameHeader.ACK:
            {
                this = ACK;
                break;
            }
            case FrameHeader.NAK:
            {
                this = NAK;
                break;
            }
            case FrameHeader.CAN:
            {
                this = CAN;
                break;
            }
            case FrameHeader.SOF:
            {
                _data = data;
                Type = FrameType.Data;
                break;
            }
            default:
            {
                throw new ArgumentException($"Frame data had unknown frame header: {data[0]}", nameof(data));
            }
        }

        // Sanity checks for a complete frame. Note that the frame way still be invalid, eg. data frame with bad checksum.
        if (Type == FrameType.Data)
        {
            // The length doesn't include the SOF or checksum, so add 2
            if (data[1] + 2 != data.Length)
            {
                throw new ArgumentException($"The data frame's length field had invalid value {data[1]} for frame data length {data.Length}", nameof(data));
            }
        }
        else if (data.Length > 1)
        {
            throw new ArgumentException($"Frame data must be exactly 1 byte for {Type} frame", nameof(data));
        }
    }

    // Used for well-known single-byte frame types
    private Frame(byte frameHeader, FrameType frameType)
    {
        _data = new[] { frameHeader };
        Type = frameType;
    }

    public static Frame ACK { get; } = new Frame(FrameHeader.ACK, FrameType.ACK);

    public static Frame NAK { get; } = new Frame(FrameHeader.NAK, FrameType.NAK);

    public static Frame CAN { get; } = new Frame(FrameHeader.CAN, FrameType.CAN);

    public FrameType Type { get; }

    public DataFrame ToDataFrame()
        => Type == FrameType.Data
            ? new DataFrame(_data)
            : throw new InvalidOperationException($"{Type} frames are not data frames");

    public override bool Equals(object? obj) => obj is Frame other && this.Equals(other);

    public bool Equals(Frame other)
        => _data.AsSpan().SequenceEqual(other._data.AsSpan());

    public static bool operator ==(Frame lhs, Frame rhs) => lhs.Equals(rhs);

    public static bool operator !=(Frame lhs, Frame rhs) => !(lhs == rhs);

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.AddBytes(_data);
        return hash.ToHashCode();
    }
}

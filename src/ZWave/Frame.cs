namespace ZWave;

internal struct Frame : IEquatable<Frame>
{
    private readonly byte[] _data;

    public Frame(byte[] data)
    {
        if (data.Length == 0)
        {
            throw new ArgumentException("Data must not be empty", nameof(data));
        }

        _data = data;

        Type = data[0] switch
        {
            FrameHeader.ACK => FrameType.ACK,
            FrameHeader.NAK => FrameType.NAK,
            FrameHeader.CAN => FrameType.CAN,
            FrameHeader.SOF => FrameType.Data,
            _ => throw new ArgumentException($"Data had unknown first byte: {data[0]}", nameof(data))
        };

        if (Type != FrameType.Data && data.Length > 1)
        {
            throw new ArgumentException($"Data must be exactly 1 byte for {Type} frame", nameof(data));
        }
    }

    public static Frame ACK { get; } = new Frame(new[] { FrameHeader.ACK });

    public static Frame NAK { get; } = new Frame(new[] { FrameHeader.NAK });

    public static Frame CAN { get; } = new Frame(new[] { FrameHeader.CAN });

    public FrameType Type { get; }

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

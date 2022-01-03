namespace ZWave.CommandClasses;

/// <summary>
/// An interpreted value from or for a node
/// </summary>
/// <remarks>
/// As defined by SDS13781 Table 21
/// </remarks>
internal struct BasicValue
{
    public BasicValue(byte value)
    {
        Value = value;
    }

    public BasicValue(int level)
    {
        if (level < 0 || level > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The value must be in the range [0..100]");
        }

        Value = level == 100 ? (byte)0xff : (byte)level;
    }

    public BasicValue(bool state)
    {
        Value = state ? (byte)0xff : (byte)0;
    }

    public byte Value { get; }

    public int? Level => Value switch
    {
        <= 99 => Value,
        0xfe => null, // Unknown
        0xff => 100,
        _ => null, // Reserved. Treat as unknown
    };

    public bool? State => Value switch
    {
        0 => false,
        <= 99 => true,
        0xfe => null, // Unknown
        0xff => true,
        _ => null, // Reserved. Treat as unknown
    };

    public static implicit operator BasicValue(byte b) => new BasicValue(b);
}

internal struct BasicCommandClassSet : ICommandClass<BasicCommandClassSet>
{
    public BasicCommandClassSet(ReadOnlyMemory<byte> payload)
    {
        Payload = payload;
    }

    public static CommandClassId CommandClassId => CommandClassId.Basic;

    public ReadOnlyMemory<byte> Payload { get; }

    public BasicValue Value => Payload.Span[0];

    public static BasicCommandClassSet Create(ReadOnlyMemory<byte> payload)
        => new BasicCommandClassSet(payload);
}

internal struct BasicCommandClassGet : ICommandClass<BasicCommandClassGet>
{
    public BasicCommandClassGet(ReadOnlyMemory<byte> payload)
    {
        Payload = payload;
    }

    public static CommandClassId CommandClassId => CommandClassId.Basic;

    public ReadOnlyMemory<byte> Payload { get; }

    public static BasicCommandClassGet Create(ReadOnlyMemory<byte> payload)
        => new BasicCommandClassGet(payload);
}

internal struct BasicCommandClassReport : ICommandClass<BasicCommandClassReport>
{
    public BasicCommandClassReport(ReadOnlyMemory<byte> payload)
    {
        Payload = payload;
    }

    public static CommandClassId CommandClassId => CommandClassId.Basic;

    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// The current value of the device hardware
    /// </summary>
    public BasicValue CurrentValue => Payload.Span[0];

    /// <summary>
    /// The the target value of an ongoing transition or the most recent transition.
    /// </summary>
    public BasicValue? TargetValue => Payload.Length > 1
        ? Payload.Span[1]
        : null;

    /// <summary>
    /// The time needed to reach the Target Value at the actual transition rate.
    /// </summary>
    public DurationReport? Duration => Payload.Length > 2
        ? Payload.Span[2]
        : null;

    public static BasicCommandClassReport Create(ReadOnlyMemory<byte> payload)
        => new BasicCommandClassReport(payload);
}

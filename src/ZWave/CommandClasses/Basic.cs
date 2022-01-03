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

enum BasicCommand
{
    /// <summary>
    /// Set a value in a supporting device
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the status of a supporting device
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the status of the primary functionality of the device.
    /// </summary>
    Report = 0x03,
}

internal struct BasicSetCommand : ICommandClass<BasicSetCommand>
{
    public BasicSetCommand(ReadOnlyMemory<byte> payload)
    {
        Payload = payload;
    }

    public static CommandClassId CommandClassId => CommandClassId.Basic;

    public ReadOnlyMemory<byte> Payload { get; }

    // TODO: Shouldn't be raw payload. Command class and command should be sliced off.
    public BasicValue Value => Payload.Span[2];

    public static BasicSetCommand Create(ReadOnlyMemory<byte> payload)
        => new BasicSetCommand(payload);
}

internal struct BasicGetCommand : ICommandClass<BasicGetCommand>
{
    public BasicGetCommand(ReadOnlyMemory<byte> payload)
    {
        Payload = payload;
    }

    public static CommandClassId CommandClassId => CommandClassId.Basic;

    public ReadOnlyMemory<byte> Payload { get; }

    public static BasicGetCommand Create(ReadOnlyMemory<byte> payload)
        => new BasicGetCommand(payload);
}

internal struct BasicReportCommand : ICommandClass<BasicReportCommand>
{
    public BasicReportCommand(ReadOnlyMemory<byte> payload)
    {
        Payload = payload;
    }

    public static CommandClassId CommandClassId => CommandClassId.Basic;

    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// The current value of the device hardware
    /// </summary>
    public BasicValue CurrentValue => Payload.Span[2];

    /// <summary>
    /// The the target value of an ongoing transition or the most recent transition.
    /// </summary>
    public BasicValue? TargetValue => Payload.Length > 3
        ? Payload.Span[3]
        : null;

    /// <summary>
    /// The time needed to reach the Target Value at the actual transition rate.
    /// </summary>
    public DurationReport? Duration => Payload.Length > 4
        ? Payload.Span[4]
        : null;

    public static BasicReportCommand Create(ReadOnlyMemory<byte> payload)
        => new BasicReportCommand(payload);
}

namespace ZWave.CommandClasses;

/// <summary>
/// An interpreted value from or for a node
/// </summary>
/// <remarks>
/// As defined by SDS13781 Table 21
/// </remarks>
public struct BasicValue
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

    public static implicit operator BasicValue(int i) => new BasicValue(i);

    public static implicit operator BasicValue(bool b) => new BasicValue(b);
}

[CommandClass(CommandClassId.Basic)]
public sealed class BasicCommandClass : CommandClass
{
    internal BasicCommandClass(
        CommandClassInfo info,
        Driver driver,
        Node node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// The current value of the device hardware
    /// </summary>
    public BasicValue? CurrentValue { get; private set; }

    /// <summary>
    /// The the target value of an ongoing transition or the most recent transition.
    /// </summary>
    public BasicValue? TargetValue { get; private set; }

    /// <summary>
    /// The time needed to reach the Target Value at the actual transition rate.
    /// </summary>
    public DurationReport? Duration { get; private set; }

    /// <summary>
    /// Request the status of a supporting device
    /// </summary>
    public async Task GetAsync(CancellationToken cancellationToken)
    {
        var command = BasicGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<BasicReportCommand>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Set a value in a supporting device
    /// </summary>
    public async Task SetAsync(BasicValue targetValue, CancellationToken cancellationToken)
    {
        var command = BasicSetCommand.Create(targetValue);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((BasicCommand)frame.CommandId)
        {
            case BasicCommand.Set:
            case BasicCommand.Get:
            {
                // We don't expect to recieve these commands
                break;
            }
            case BasicCommand.Report:
            {
                var command = new BasicReportCommand(frame);
                CurrentValue = command.CurrentValue;
                TargetValue = command.TargetValue;
                Duration = command.Duration;
                break;
            }
        }
    }

    private enum BasicCommand
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

    private struct BasicSetCommand : ICommand<BasicSetCommand>
    {
        public BasicSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Basic;

        public static byte CommandId => (byte)BasicCommand.Set;

        public CommandClassFrame Frame { get; }

        public BasicValue Value => Frame.CommandParameters.Span[0];

        public static BasicSetCommand Create(BasicValue value)
        {
            Span<byte> commandParameters = stackalloc byte[1];
            commandParameters[0] = value.Value;

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new BasicSetCommand(frame);
        }
    }

    private struct BasicGetCommand : ICommand<BasicGetCommand>
    {
        public BasicGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Basic;

        public static byte CommandId => (byte)BasicCommand.Get;

        public CommandClassFrame Frame { get; }

        public static BasicGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BasicGetCommand(frame);
        }
    }

    private struct BasicReportCommand : ICommand<BasicReportCommand>
    {
        public BasicReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Basic;

        public static byte CommandId => (byte)BasicCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The current value of the device hardware
        /// </summary>
        public BasicValue CurrentValue => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The the target value of an ongoing transition or the most recent transition.
        /// </summary>
        public BasicValue? TargetValue => Frame.CommandParameters.Length > 1
            ? Frame.CommandParameters.Span[1]
            : null;

        /// <summary>
        /// The time needed to reach the Target Value at the actual transition rate.
        /// </summary>
        public DurationReport? Duration => Frame.CommandParameters.Length > 2
            ? Frame.CommandParameters.Span[2]
            : null;
    }
}

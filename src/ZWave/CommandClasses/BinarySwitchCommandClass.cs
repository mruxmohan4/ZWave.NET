namespace ZWave.CommandClasses;

[CommandClass(CommandClassId.BinarySwitch)]
internal class BinarySwitchCommandClass : CommandClass
{
    public BinarySwitchCommandClass(CommandClassInfo info, Driver driver, Node node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// The current On/Off state at the sending node
    /// </summary>
    public bool? CurrentValue { get; private set; }

    /// <summary>
    /// The target value of an ongoing transition or the most recent transition.
    /// </summary>
    public bool? TargetValue { get; private set; }

    /// <summary>
    /// Advertise the duration of a transition from the Current Value to the Target Value.
    /// </summary>
    public DurationReport? Duration { get; private set; }

    /// <summary>
    /// Request the current On/Off state from a node
    /// </summary>
    public async Task GetAsync(CancellationToken cancellationToken)
    {
        var command = BinarySwitchGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<BinarySwitchReportCommand>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Set the On/Off state at the receiving node.
    /// </summary>
    public async Task SetAsync(
        bool targetValue,
        DurationSet? duration,
        CancellationToken cancellationToken)
    {
        var command = BinarySwitchSetCommand.Create(targetValue, duration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((BinarySwitchCommand)frame.CommandId)
        {
            case BinarySwitchCommand.Set:
            case BinarySwitchCommand.Get:
            {
                // We don't expect to recieve these commands
                break;
            }
            case BinarySwitchCommand.Report:
            {
                var command = new BinarySwitchReportCommand(frame);
                CurrentValue = command.CurrentValue;
                TargetValue = command.TargetValue;
                Duration = command.Duration;
                break;
            }
        }
    }

    private enum BinarySwitchCommand
    {
        /// <summary>
        /// Set the On/Off state at the receiving node.
        /// </summary>
        Set = 0x01,

        /// <summary>
        /// Request the current On/Off state from a node
        /// </summary>
        Get = 0x02,

        /// <summary>
        /// Advertise the current On/Off state at the sending node
        /// </summary>
        Report = 0x03,
    }

    private struct BinarySwitchSetCommand : ICommand<BinarySwitchSetCommand>
    {
        public BinarySwitchSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Basic;

        public static byte CommandId => (byte)BinarySwitchCommand.Set;

        public CommandClassFrame Frame { get; }

        public BasicValue Value => Frame.CommandParameters.Span[0];

        public static BinarySwitchSetCommand Create(bool value, DurationSet? duration)
        {
            Span<byte> commandParameters = stackalloc byte[1 + (duration.HasValue ? 1 : 0)];
            commandParameters[0] = value ? (byte)0xff : (byte)0x00;
            if (duration.HasValue)
            {
                commandParameters[1] = duration.Value.Value;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new BinarySwitchSetCommand(frame);
        }
    }

    private struct BinarySwitchGetCommand : ICommand<BinarySwitchGetCommand>
    {
        public BinarySwitchGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Basic;

        public static byte CommandId => (byte)BinarySwitchCommand.Get;

        public CommandClassFrame Frame { get; }

        public static BinarySwitchGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new BinarySwitchGetCommand(frame);
        }
    }

    private struct BinarySwitchReportCommand : ICommand<BinarySwitchReportCommand>
    {
        public BinarySwitchReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Basic;

        public static byte CommandId => (byte)BinarySwitchCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The current On/Off state at the sending node
        /// </summary>
        public bool? CurrentValue => ParseBool(Frame.CommandParameters.Span[0]);

        /// <summary>
        /// The the target value of an ongoing transition or the most recent transition.
        /// </summary>
        public bool? TargetValue => Frame.CommandParameters.Length > 1
            ? ParseBool(Frame.CommandParameters.Span[1])
            : null;

        /// <summary>
        /// The time needed to reach the Target Value at the actual transition rate.
        /// </summary>
        public DurationReport? Duration => Frame.CommandParameters.Length > 2
            ? Frame.CommandParameters.Span[2]
            : null;

        private static bool? ParseBool(byte b)
            => b switch
            {
                0x00 => false,
                0xff => true,
                _ => null,
            };
    }
}

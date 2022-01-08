namespace ZWave.CommandClasses;

internal enum NotImplementedCommand : byte
{
}

/// <summary>
/// Represents a command class which hasn't been implemented by this library yet.
/// </summary>
internal sealed class NotImplementedCommandClass : CommandClass<NotImplementedCommand>
{
    public NotImplementedCommandClass(CommandClassInfo info, Driver driver, Node node) : base(info, driver, node)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(NotImplementedCommand command) => false;

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
    }
}

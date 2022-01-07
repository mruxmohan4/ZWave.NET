namespace ZWave.CommandClasses;

/// <summary>
/// Represents a command class which hasn't been implemented by this library yet.
/// </summary>
internal sealed class NotImplementedCommandClass : CommandClass
{
    public NotImplementedCommandClass(CommandClassInfo info, Driver driver, Node node) : base(info, driver, node)
    {
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
    }
}

namespace ZWave.CommandClasses;

public abstract class CommandClass
{
    private record struct AwaitedReport(byte CommandId, TaskCompletionSource TaskCompletionSource);

    private readonly Driver _driver;

    // We don't expect this to get very large at all, so using a simple list to save on memory instead
    // of Dictionary<CommandId, List<TCS>> which would have faster lookups
    private readonly List<AwaitedReport> _awaitedReports = new List<AwaitedReport>();

    private Task? _initializeTask;

    internal CommandClass(
        CommandClassInfo info,
        Driver driver,
        Node node)
    {
        Info = info;
        _driver = driver;
        Node = node;
    }

    public CommandClassInfo Info { get; private set; }

    public Node Node { get; }

    public byte? Version { get; private set; }

    internal void MergeInfo(CommandClassInfo info)
    {
        if (info.CommandClass != Info.CommandClass)
        {
            throw new ArgumentException($"Frame is for the wrong command class. Expected {Info.CommandClass} but found {info.CommandClass}.", nameof(info));
        }

        Info = new CommandClassInfo(
            info.CommandClass,
            info.IsSupported || Info.IsSupported,
            info.IsControlled || Info.IsControlled);
    }

    internal void SetVersion(byte version)
    {
        Version = version;
    }

    internal Task InitializeAsync(CancellationToken cancellationToken)
    {
        _initializeTask = InitializeCoreAsync(cancellationToken);
        return _initializeTask;
    }

    protected virtual Task InitializeCoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal Task WaitForInitializedAsync() => _initializeTask ?? throw new InvalidOperationException("The command class has not begun initialization yet");

    internal void ProcessCommand(CommandClassFrame frame)
    {
        if (frame.CommandClassId != Info.CommandClass)
        {
            throw new ArgumentException($"Frame is for the wrong command class. Expected {Info.CommandClass} but found {frame.CommandClassId}.", nameof(frame));
        }

        lock (_awaitedReports)
        {
            int i = 0;
            while (i < _awaitedReports.Count)
            {
                AwaitedReport awaitedReport = _awaitedReports[i];
                if (awaitedReport.CommandId == frame.CommandId)
                {
                    awaitedReport.TaskCompletionSource.SetResult();
                    _awaitedReports.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        ProcessCommandCore(frame);
    }

    protected abstract void ProcessCommandCore(CommandClassFrame frame);

    internal async Task SendCommandAsync<TRequest>(
        TRequest command,
        CancellationToken cancellationToken)
        where TRequest : struct, ICommand<TRequest>
        => await _driver.SendCommandAsync(command, Node.Id, cancellationToken).ConfigureAwait(false);

    internal async Task AwaitNextReportAsync<TReport>(CancellationToken cancellationToken)
        where TReport : struct, ICommand<TReport>
    {
        if (TReport.CommandClassId != Info.CommandClass)
        {
            throw new ArgumentException($"Report is for the wrong command class. Expected {Info.CommandClass} but found {TReport.CommandClassId}.", nameof(TReport));
        }

        var tcs = new TaskCompletionSource();
        var awaitedReport = new AwaitedReport(TReport.CommandId, tcs);
        lock (_awaitedReports)
        {
            _awaitedReports.Add(awaitedReport);
        }

        using (cancellationToken.Register(static state => ((TaskCompletionSource)state!).TrySetCanceled(), tcs))
        {
            await tcs.Task;
        }
    }
}

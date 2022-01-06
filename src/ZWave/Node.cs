using Microsoft.Extensions.Logging;
using ZWave.CommandClasses;
using ZWave.Serial.Commands;

namespace ZWave;

public sealed class Node
{
    private readonly Driver _driver;

    private readonly ILogger _logger;

    private readonly AsyncAutoResetEvent _nodeInfoRecievedEvent = new AsyncAutoResetEvent();

    private readonly Dictionary<CommandClassId, CommandClass> _commandClasses = new Dictionary<CommandClassId, CommandClass>();

    private Task? _interviewTask;

    private CancellationTokenSource? _interviewCancellationTokenSource;

    internal Node(byte id, Driver driver, ILogger logger)
    {
        Id = id;
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public byte Id { get; }

    public bool IsListening { get; private set; }

    public bool IsRouting { get; private set; }

    public IReadOnlyList<int> SupportedSpeeds { get; private set; } = Array.Empty<int>();

    public byte ProtocolVersion { get; private set; }

    public NodeType NodeType { get; private set; }

    public FrequentListeningMode FrequentListeningMode { get; private set; }

    public bool SupportsBeaming { get; private set; }

    public bool SupportsSecurity { get; private set; }

    public IReadOnlyDictionary<CommandClassId, CommandClassInfo> CommandClasses
    {
        get
        {
            var commandClassInfos = new Dictionary<CommandClassId, CommandClassInfo>(_commandClasses.Count);
            foreach (KeyValuePair<CommandClassId, CommandClass> pair in _commandClasses)
            {
                commandClassInfos.Add(pair.Key, pair.Value.Info);
            }

            return commandClassInfos;
        }
    }

    public TCommandClass GetCommandClass<TCommandClass>()
        where TCommandClass : CommandClass
    {
        CommandClassId commandClassId = CommandClassFactory.GetCommandClassId<TCommandClass>();

        if (!_commandClasses.TryGetValue(commandClassId, out CommandClass? commandClass))
        {
            throw new ZWaveException(ZWaveErrorCode.CommandClassNotSupported, $"The command class {commandClassId} is not supported by this node.");
        }

        return (TCommandClass)commandClass;
    }

    /// <summary>
    /// Interviews a node.
    /// </summary>
    /// <remarks>
    /// the interview may take a very long time, so the returned task should generally not be awaited.
    /// </remarks>
    internal async Task InterviewAsync(CancellationToken cancellationToken)
    {
        // Cancel any preview interview
        if (_interviewCancellationTokenSource != null)
        {
            _interviewCancellationTokenSource.Cancel();
        }

        // Wait for any preview interview to stop
        if (_interviewTask != null)
        {
            await _interviewTask;
        }

        _interviewCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _interviewTask = Task.Run(async () =>
        {
            try
            {
                CancellationToken cancellationToken = _interviewCancellationTokenSource.Token;

                var getNodeProtocolInfoRequest = GetNodeProtocolInfoRequest.Create(Id);
                GetNodeProtocolInfoResponse getNodeProtocolInfoResponse = await _driver.SendCommandAsync<GetNodeProtocolInfoRequest, GetNodeProtocolInfoResponse>(
                    getNodeProtocolInfoRequest,
                    cancellationToken).ConfigureAwait(false);
                IsListening = getNodeProtocolInfoResponse.IsListening;
                IsRouting = getNodeProtocolInfoResponse.IsRouting;
                SupportedSpeeds = getNodeProtocolInfoResponse.SupportedSpeeds;
                ProtocolVersion = getNodeProtocolInfoResponse.ProtocolVersion;
                NodeType = getNodeProtocolInfoResponse.NodeType;
                FrequentListeningMode = getNodeProtocolInfoResponse.FrequentListeningMode;
                SupportsBeaming = getNodeProtocolInfoResponse.SupportsBeaming;
                SupportsSecurity = getNodeProtocolInfoResponse.SupportsSecurity;
                // TODO: Log

                // We don't need to query some information for the controller node
                if (Id != _driver.Controller.NodeId)
                {
                    // This request causes unsolicited requests from the controller (kind of like a callback
                    // with command id ApplicationControllerUpdate
                    var requestNodeInfoRequest = RequestNodeInfoRequest.Create(Id);
                    await _driver.SendCommandAsync(requestNodeInfoRequest, cancellationToken)
                        .ConfigureAwait(false);
                    await _nodeInfoRecievedEvent.WaitAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

                    // TODO: Query command classes
                }
            }
            catch (OperationCanceledException)
            {
            }
        },
        cancellationToken);
    }

    internal void NotifyNodeInfoReceived(ApplicationUpdateRequest nodeInfoReceived)
    {
        // TODO: Log
        foreach (CommandClassInfo commandClassInfo in nodeInfoReceived.Generic.CommandClasses)
        {
            AddCommandClass(commandClassInfo);
        }

        _nodeInfoRecievedEvent.Set();
    }

    private void AddCommandClass(CommandClassInfo commandClassInfo)
    {
        lock(_commandClasses)
        {
            if (_commandClasses.TryGetValue(commandClassInfo.CommandClass, out CommandClass? existingCommandClass))
            {
                existingCommandClass.MergeInfo(commandClassInfo);
            }
            else
            {
                CommandClass? commandClass = CommandClassFactory.Create(commandClassInfo, _driver, this);

                // If null, that means we just haven't implemented it yet.
                if (commandClass != null)
                {
                    _commandClasses.Add(commandClassInfo.CommandClass, commandClass);
                }
            }
        }
    }

    internal void ProcessCommand(CommandClassFrame frame)
    {
        if (_commandClasses.TryGetValue(frame.CommandClassId, out CommandClass? commandClass))
        {
            commandClass.ProcessCommand(frame);
        }
        else
        {
            // TODO: Log
        }
    }
}

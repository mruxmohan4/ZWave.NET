using System.Threading;

using Microsoft.Extensions.Logging;

using ZWave.Commands;

namespace ZWave;

public sealed class Node
{
    private readonly Driver _driver;

    private readonly ILogger _logger;

    private readonly AsyncAutoResetEvent _nodeInfoRecievedEvent = new AsyncAutoResetEvent();

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
        });
    }

    internal void NotifyNodeInfoReceived(ApplicationControllerUpdateNodeInfoReceived nodeInfoReceived)
    {
        // TODO: Do something with the data

        _nodeInfoRecievedEvent.Set();
    }
}

using Microsoft.Extensions.Logging;

using ZWave.Serial;

namespace ZWave;

public sealed class ZWaveDriver
{
    private readonly ILogger _logger;

    public ZWaveDriver(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ConnectAsync(string portName, CancellationToken cancellationToken)
    {
        var stream = new ZWaveSerialPortStream(_logger, portName);
        var stateMachine = new ZWaveStateMachine(_logger, stream);
        await stateMachine.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}

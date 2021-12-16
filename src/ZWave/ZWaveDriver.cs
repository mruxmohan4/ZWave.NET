using ZWave.Serial;

namespace ZWave;

public sealed class ZWaveDriver
{
    public ZWaveDriver()
    {
    }

    public async Task ConnectAsync(string portName, CancellationToken cancellationToken)
    {
        var stream = new ZWaveSerialPortStream(portName);
        var stateMachine = new ZWaveStateMachine(stream);
        await stateMachine.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}

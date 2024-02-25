using Microsoft.Extensions.Logging;
using ZWave.CommandClasses;
using ZWave.Serial.Commands;

namespace ZWave.Tests.Mocks;

internal class MockDriver : IDriver
{
    private readonly ILogger _logger;
    public MockDriver(ILogger logger)
    {
        Controller =  new Controller(logger, this);
        SentCommands = new List<ReadOnlyMemory<byte>>();
        _logger = logger;
    }

    public Controller Controller { get; }
    public List<ReadOnlyMemory<byte>> SentCommands { get; }

    public Task SendCommandAsync<TCommand>(
        TCommand request,
        byte id,
        CancellationToken cancellationToken)
        where TCommand : struct, ICommand
    {
        SentCommands.Add(request.Frame.Data);
        return Task.CompletedTask;
    }

    // Mock sending a response from the supporting device.
    public static void MockResponse(CommandClass commandClass, byte commandId, ReadOnlyMemory<byte> responseData)
    {
        switch(commandId)
        {
            case 0x01: // Set - do nothing
                break;
            case 0x02: // Get - expect a report of the current state
                var commandClassFrame = new CommandClassFrame(responseData);
                commandClass.ProcessCommand(commandClassFrame);
                break;
            case 0x03: // Report
                break;
            default:
                break;
        }
    }

    public byte GetNextSessionId()
    {
        throw new NotImplementedException();
    }

    public Task<TResponse> SendCommandAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : struct, ICommand<TRequest>
        where TResponse : struct, ICommand<TResponse>
    {
        throw new NotImplementedException();
    }

    public Task<TCallback> SendCommandExpectingCallbackAsync<TRequest, TCallback>(TRequest request, CancellationToken cancellationToken)
        where TRequest : struct, IRequestWithCallback<TRequest>
        where TCallback : struct, ICommand<TCallback>
    {
        throw new NotImplementedException();
    }
}
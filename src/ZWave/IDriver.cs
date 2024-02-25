using ZWave.CommandClasses;
using ZWave.Serial.Commands;

namespace ZWave;

public interface IDriver
{
    public Controller Controller { get; }
    byte GetNextSessionId();
    Task SendCommandAsync<TCommand>(TCommand request, byte id, CancellationToken cancellationToken)
        where TCommand : struct, ICommand;
    Task<TResponse> SendCommandAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : struct, ICommand<TRequest>
        where TResponse : struct, ICommand<TResponse>;
    Task<TCallback> SendCommandExpectingCallbackAsync<TRequest, TCallback>(TRequest request, CancellationToken cancellationToken)
        where TRequest : struct, IRequestWithCallback<TRequest>
        where TCallback : struct, ICommand<TCallback>;
}
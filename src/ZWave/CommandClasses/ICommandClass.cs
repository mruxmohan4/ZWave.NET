namespace ZWave.CommandClasses;

internal interface ICommandClass<TCommandClass> where TCommandClass : struct, ICommandClass<TCommandClass>
{
    public static abstract CommandClassId CommandClassId { get; }

    public static abstract TCommandClass Create(ReadOnlyMemory<byte> payload);

    public ReadOnlyMemory<byte> Payload { get; }
}

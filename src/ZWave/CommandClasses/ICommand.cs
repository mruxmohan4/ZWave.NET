namespace ZWave.CommandClasses;

internal interface ICommand<TCommand> where TCommand : struct, ICommand<TCommand>
{
    public static abstract CommandClassId CommandClassId { get; }

    public static abstract byte CommandId { get; }

    public static abstract TCommand Create(CommandClassFrame frame);

    public CommandClassFrame Frame { get; }
}

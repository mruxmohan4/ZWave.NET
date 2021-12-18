using ZWave.Serial;

namespace ZWave.Commands;

internal interface ICommand
{
    public static abstract DataFrameType Type { get; }

    public static abstract CommandId CommandId { get; }

    public DataFrame Frame { get; }
}
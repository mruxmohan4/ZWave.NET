using System.Xml.Linq;

namespace ZWave.CommandClasses;

// TODO: This should be generated
internal static class CommandClassFactory
{
    private static readonly Dictionary<CommandClassId, Func<CommandClassInfo, Driver, Node, CommandClass>> Constructors = new Dictionary<CommandClassId, Func<CommandClassInfo, Driver, Node, CommandClass>>
    {
        { CommandClassId.Basic, (info, driver, node) => new BasicCommandClass(info, driver, node) },
    };

    private static readonly Dictionary<Type, CommandClassId> TypeToIdMap = new Dictionary<Type, CommandClassId>
    {
        { typeof(BasicCommandClass), CommandClassId.Basic },
    };

    public static CommandClass? Create(CommandClassInfo info, Driver driver, Node node)
        => Constructors.TryGetValue(info.CommandClass, out Func<CommandClassInfo, Driver, Node, CommandClass>? constructor)
            ? constructor(info, driver, node)
            : null;

    public static CommandClassId GetCommandClassId<TCommandClass>()
        where TCommandClass : CommandClass
        => TypeToIdMap[typeof(TCommandClass)];
}

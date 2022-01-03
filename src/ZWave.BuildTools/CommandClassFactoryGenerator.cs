using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZWave.BuildTools;

#pragma warning disable RS2008 // Enable analyzer release tracking. These are internal to the repo; we don't release them.

[Generator]
public sealed class CommandClassFactoryGenerator : ISourceGenerator
{
    private static readonly DiagnosticDescriptor DuplicateCommandClassId = new DiagnosticDescriptor(
        id: "ZWAVE001",
        title: "Found duplicate command id",
        messageFormat: "Found multiple classes claiming to handle the command class '{0}'",
        category: "ZWave.BuildTools",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    public void Initialize(GeneratorInitializationContext context)
    {
        const string attributeSource = @"
namespace ZWave.CommandClasses;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal sealed class CommandClassAttribute: Attribute
{
    public CommandClassId Id { get; }

    public CommandClassAttribute(CommandClassId id)
        => (Id) = (id);
}
";
        context.RegisterForPostInitialization((pi) => pi.AddSource("CommandClassAttribute.generated.cs", attributeSource));
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        SyntaxReceiver syntaxReceiver = (SyntaxReceiver)context.SyntaxReceiver!;

        foreach (Diagnostic diagnostic in syntaxReceiver.DiagnosticsToReport)
        {
            context.ReportDiagnostic(diagnostic);
        }

        Dictionary<string, string> commandClassIdToType = syntaxReceiver.CommandClassIdToType;

        var sb = new StringBuilder();
        sb.Append(@"
#nullable enable

namespace ZWave.CommandClasses;

internal static class CommandClassFactory
{
    private static readonly Dictionary<CommandClassId, Func<CommandClassInfo, Driver, Node, CommandClass>> Constructors = new Dictionary<CommandClassId, Func<CommandClassInfo, Driver, Node, CommandClass>>
    {
");

        foreach (KeyValuePair<string, string> pair in commandClassIdToType)
        {
            string commandClassId = pair.Key;
            string commandClassType = pair.Value;

            // { CommandClassId.Basic, (info, driver, node) => new BasicCommandClass(info, driver, node) },
            sb.Append("        { ");
            sb.Append(commandClassId);
            sb.Append(", (info, driver, node) => new ");
            sb.Append(commandClassType);
            sb.Append("(info, driver, node) },");
            sb.AppendLine();
        }

        sb.Append(@"    };

    private static readonly Dictionary<Type, CommandClassId> TypeToIdMap = new Dictionary<Type, CommandClassId>
    {
");

        foreach (KeyValuePair<string, string> pair in commandClassIdToType)
        {
            string commandClassId = pair.Key;
            string commandClassType = pair.Value;

            // { typeof(BasicCommandClass), CommandClassId.Basic },
            sb.Append("        { typeof(");
            sb.Append(commandClassType);
            sb.Append("), ");
            sb.Append(commandClassId);
            sb.Append(" },");
            sb.AppendLine();
        }

        sb.Append(@"    };

    public static CommandClass? Create(CommandClassInfo info, Driver driver, Node node)
        => Constructors.TryGetValue(info.CommandClass, out Func<CommandClassInfo, Driver, Node, CommandClass>? constructor)
            ? constructor(info, driver, node)
            : null;

    public static CommandClassId GetCommandClassId<TCommandClass>()
        where TCommandClass : CommandClass
        => TypeToIdMap[typeof(TCommandClass)];
}
");

        context.AddSource("CommandClassFactory.generated.cs", sb.ToString());
    }

    private sealed class SyntaxReceiver : ISyntaxReceiver
    {
        public Dictionary<string, string> CommandClassIdToType { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public List<Diagnostic> DiagnosticsToReport { get; } = new List<Diagnostic>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
            {
                foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
                {
                    foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                    {
                        var attributeName = attributeSyntax.Name.ToString();
                        if (attributeName == "CommandClass"
                            || attributeName == "CommandClassAttribute")
                        {
                            if (attributeSyntax.ArgumentList != null
                                && attributeSyntax.ArgumentList.Arguments.Count > 0)
                            {
                                AttributeArgumentSyntax attributeArgumentSyntax = attributeSyntax.ArgumentList.Arguments[0];
                                string attributeArgumentValue = attributeArgumentSyntax.ToString();
                                if (attributeArgumentValue.StartsWith("CommandClassId."))
                                {
                                    if (CommandClassIdToType.ContainsKey(attributeArgumentValue))
                                    {
                                        var diagnostic = Diagnostic.Create(
                                            DuplicateCommandClassId,
                                            attributeSyntax.GetLocation(),
                                            attributeArgumentValue);
                                        DiagnosticsToReport.Add(diagnostic);
                                    }
                                    else
                                    {
                                        CommandClassIdToType.Add(attributeArgumentValue, classDeclarationSyntax.Identifier.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private record struct CommandClassMetadata(string CommandClassId, string CommandClassType);
}

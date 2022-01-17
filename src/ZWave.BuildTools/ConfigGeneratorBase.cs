using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ZWave.BuildTools;

public abstract class ConfigGeneratorBase<TConfig> : ISourceGenerator
{
    private static readonly DiagnosticDescriptor MissingConfig = new DiagnosticDescriptor(
        id: "ZWAVE002",
        title: "Missing config file",
        messageFormat: "Could not find file for config type '{0}'",
        category: "ZWave.BuildTools",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidConfig = new DiagnosticDescriptor(
        id: "ZWAVE003",
        title: "Invalid config file",
        messageFormat: "Config type '{0}' was invalid: {1}",
        category: "ZWave.BuildTools",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    protected abstract string ConfigType { get; }

    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization required
    }

    public void Execute(GeneratorExecutionContext context)
{
        AdditionalText? configFile = GetMatchingConfigFile(context);
        if (configFile == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(MissingConfig, Location.None, ConfigType));
            return;
        }

        string configContent = configFile.GetText()!.ToString();

        TConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<TConfig>(
                configContent,
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                });
        }
        catch (JsonException ex)
        {
            Location location;
            if (ex.LineNumber == null || ex.BytePositionInLine == null)
            {
                location = Location.None;
            }
            else
            {
                var linePosition = new LinePosition((int)ex.LineNumber.Value, (int)ex.BytePositionInLine.Value);
                var lineSpan = new LinePositionSpan(linePosition, linePosition);
                var textSpan = new TextSpan(0, 0);
                location = Location.Create(configFile.Path, textSpan, lineSpan);
            }

            var diagnostic = Diagnostic.Create(InvalidConfig, location, ConfigType, ex.Message);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        if (config == null)
        {
            var diagnostic = Diagnostic.Create(InvalidConfig, Location.None, ConfigType, "Json was invalid");
            context.ReportDiagnostic(diagnostic);
            return;
        }

        string source = CreateSource(config);
        context.AddSource(ConfigType + ".generated.cs", source);
    }

    protected abstract string CreateSource(TConfig config);

    private AdditionalText? GetMatchingConfigFile(GeneratorExecutionContext context)
    {
        foreach (AdditionalText file in context.AdditionalFiles)
        {
            if (context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.additionalfiles.ConfigType", out string? configType)
                && configType.Equals(ConfigType))
            {
                return file;
            }
        }

        return null;
    }
}

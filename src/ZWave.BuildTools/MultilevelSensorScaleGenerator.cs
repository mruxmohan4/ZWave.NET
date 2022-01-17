using System.Text;
using Microsoft.CodeAnalysis;

namespace ZWave.BuildTools;

[Generator]
public sealed class MultilevelSensorScaleGenerator : ConfigGeneratorBase<ScaleTypeConfigs>
{
    protected override string ConfigType => "MultilevelSensorScales";

    protected override string CreateSource(ScaleTypeConfigs config)
    {
        var sb = new StringBuilder();
        sb.Append(@"
#nullable enable

namespace ZWave.CommandClasses;

public sealed class MultilevelSensorScale
{
    internal MultilevelSensorScale(byte id, string label, string? unit)
    {
        Id = id;
        Label = label;
        Unit = unit;
    }

    public byte Id { get; }

    public string Label { get; }

    public string? Unit { get; }

");

        foreach (ScaleTypeConfig scaleTypeConfig in config.ScaleTypes)
        {
            sb.Append($@"
    public static IReadOnlyDictionary<byte, MultilevelSensorScale> {scaleTypeConfig.Name} {{ get; }}
        = ");

            AppendScalesDictionary(sb, scaleTypeConfig.Scales, indentLevel: 2);

            sb.AppendLine(";");
        }

        sb.Append(@"
}
");
        return sb.ToString();
    }

    internal static void AppendScalesDictionary(
        StringBuilder sb,
        IReadOnlyList<ScaleConfig> scales,
        int indentLevel)
    {
        static void Indent(StringBuilder sb, int indentLevel) => sb.Append(' ', indentLevel * 4);

        // Caller is expecetd to indent the first line since there might be a prefix.
        sb.AppendLine("new Dictionary<byte, MultilevelSensorScale>");

        Indent(sb, indentLevel);
        sb.AppendLine("{");

        foreach (ScaleConfig scale in scales)
        {
            Indent(sb, indentLevel + 1);
            sb.Append($@"{{ {scale.Id}, new MultilevelSensorScale({scale.Id}, ""{scale.Label}"", ");
            if (scale.Unit == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append($@"""{scale.Unit}""");
            }

            sb.AppendLine($@") }},");
        }

        Indent(sb, indentLevel);
        sb.Append('}');
    }
}

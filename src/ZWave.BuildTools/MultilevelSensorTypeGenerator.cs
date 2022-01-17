using System.Text;
using Microsoft.CodeAnalysis;

namespace ZWave.BuildTools;

[Generator]
public sealed class MultilevelSensorTypeGenerator : ConfigGeneratorBase<SensorTypeConfigs>
{
    protected override string ConfigType => "MultilevelSensorTypes";

    protected override string CreateSource(SensorTypeConfigs config)
    {
        var sb = new StringBuilder();
        sb.Append(@"
#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace ZWave.CommandClasses;

public enum MultilevelSensorType : byte
{
");

        IReadOnlyList<SensorTypeConfig> sensorTypes = config.SensorTypes;
        foreach (SensorTypeConfig sensorType in sensorTypes)
        {
            sb.Append($@"
    {sensorType.Name} = {sensorType.Id},");
        }

        sb.Append(@"
}

public static class MultilevelSensorTypeExtensions
{
    private static readonly Dictionary<MultilevelSensorType, string> DisplayNames = new()
    {");

        foreach (SensorTypeConfig sensorType in sensorTypes)
        {
            sb.Append($@"
        {{ MultilevelSensorType.{sensorType.Name},  ""{sensorType.DisplayName}"" }},");
        }

        sb.Append(@"
    };

    public static string ToDisplayString(this MultilevelSensorType sensorType)
        => DisplayNames.TryGetValue(sensorType, out string? displayName)
            ? displayName
            : ""Unknown"";

    private static readonly Dictionary<MultilevelSensorType, IReadOnlyDictionary<byte, MultilevelSensorScale>> ScaleTypes = new()
    {");

        foreach (SensorTypeConfig sensorType in sensorTypes)
        {
            // { MultilevelSensorType.Foo, MultilevelSensorScale.Bar }
            if (sensorType.ScaleType != null)
            {
                sb.Append($@"
        {{ MultilevelSensorType.{sensorType.Name}, MultilevelSensorScale.{sensorType.ScaleType} }},");
            }

            /*
                {
                    MultiLevelSensorType.Foo,
                    new Dictionary<byte, MultilevelSensorScale>
                    {
                        ...
                    }
                }
            */
            else if (sensorType.Scales != null)
            {
                sb.Append($@"
        {{
            MultilevelSensorType.{sensorType.Name},
            ");
                MultilevelSensorScaleGenerator.AppendScalesDictionary(sb, sensorType.Scales, indentLevel: 3);
                sb.Append($@"
        }},");

            }
        }

        sb.Append(@"
    };

    public static MultilevelSensorScale GetScale(this MultilevelSensorType sensorType, byte scaleId)
        => ScaleTypes.TryGetValue(sensorType, out IReadOnlyDictionary<byte, MultilevelSensorScale>? scaleTypes)
            ? scaleTypes.TryGetValue(scaleId, out MultilevelSensorScale? scale)
                ? scale
                : new MultilevelSensorScale(scaleId, ""Unknown"", null)
            : new MultilevelSensorScale(scaleId, ""Unknown"", null);
}
");
        return sb.ToString();
    }
}

using System.Text.Json.Serialization;

namespace ZWave.BuildTools;

[JsonConverter(typeof(ScaleTypeConfigsConverter))]
public sealed class ScaleTypeConfigs
{
    public ScaleTypeConfigs(IReadOnlyList<ScaleTypeConfig> scaleTypes)
    {
        ScaleTypes = scaleTypes;
    }

    public IReadOnlyList<ScaleTypeConfig> ScaleTypes { get; }
}

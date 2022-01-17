using System.Text.Json.Serialization;

namespace ZWave.BuildTools;

[JsonConverter(typeof(SensorTypeConfigsConverter))]
public sealed class SensorTypeConfigs
{
    public SensorTypeConfigs(IReadOnlyList<SensorTypeConfig> sensorTypes)
    {
        SensorTypes = sensorTypes;
    }

    public IReadOnlyList<SensorTypeConfig> SensorTypes { get; }
}

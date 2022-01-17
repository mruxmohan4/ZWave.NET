using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZWave.BuildTools;

public sealed class SensorTypeConfigsConverter : JsonConverter<SensorTypeConfigs>
{
    public override SensorTypeConfigs? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReadSensorTypes(ref reader);

    public override void Write(Utf8JsonWriter writer, SensorTypeConfigs value, JsonSerializerOptions options)
        => throw new NotSupportedException();

    private static SensorTypeConfigs ReadSensorTypes(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected an array");
        }

        var sensorTypes = new List<SensorTypeConfig>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            SensorTypeConfig sensorType = ReadSensorType(ref reader);
            sensorTypes.Add(sensorType);
        }

        return new SensorTypeConfigs(sensorTypes);
    }

    private static SensorTypeConfig ReadSensorType(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected an object");
        }

        string? id = null;
        string? name = null;
        string? displayName = null;
        string? scaleType = null;
        IReadOnlyList<ScaleConfig>? scales = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string? propertyName = reader.GetString();

            switch (propertyName)
            {
                case nameof(id):
                {
                    reader.Read();
                    id = reader.GetString();
                    break;
                }
                case nameof(name):
                {
                    reader.Read();
                    name = reader.GetString();
                    break;
                }
                case nameof(displayName):
                {
                    reader.Read();
                    displayName = reader.GetString();
                    break;
                }
                case nameof(scaleType):
                {
                    reader.Read();
                    scaleType = reader.GetString();
                    break;
                }
                case nameof(scales):
                {
                    reader.Read();
                    scales = ScaleTypeConfigsConverter.ReadScales(ref reader);
                    break;
                }
                default:
                {
                    throw new JsonException($"Invalid property: {propertyName}");
                }
            }
        }

        if (id == null)
        {
            throw new JsonException($"Property '{nameof(id)}' is missing");
        }

        if (!CommonRegexes.ValidHexRegex.IsMatch(id))
        {
            throw new JsonException($"Invalid {nameof(id)} '{id}'");
        }

        if (name == null)
        {
            throw new JsonException($"Property '{nameof(name)}' is missing");
        }

        if (!CommonRegexes.ValidCodeSymbolRegex.IsMatch(name))
        {
            throw new JsonException($"Invalid {nameof(name)} '{name}'");
        }

        if (displayName == null)
        {
            throw new JsonException($"Property '{nameof(displayName)}' is missing");
        }

        if (!CommonRegexes.ValidStringRegex.IsMatch(displayName))
        {
            throw new JsonException($"Invalid {nameof(displayName)} '{displayName}'");
        }

        if (scaleType == null && scales == null)
        {
            throw new JsonException($"Property '{nameof(scaleType)}' is missing");
        }

        if (scaleType != null && !CommonRegexes.ValidCodeSymbolRegex.IsMatch(scaleType))
        {
            throw new JsonException($"Invalid {nameof(scaleType)} '{scaleType}'");
        }

        return new SensorTypeConfig(id, name, displayName, scaleType, scales);
    }
}

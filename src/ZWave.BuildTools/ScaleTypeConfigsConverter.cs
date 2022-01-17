using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZWave.BuildTools;

public sealed class ScaleTypeConfigsConverter : JsonConverter<ScaleTypeConfigs>
{
    public override ScaleTypeConfigs? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReadScaleTypes(ref reader);

    public override void Write(Utf8JsonWriter writer, ScaleTypeConfigs value, JsonSerializerOptions options)
        => throw new NotSupportedException();

    private static ScaleTypeConfigs ReadScaleTypes(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected an array");
        }

        var scaleTypes = new List<ScaleTypeConfig>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            ScaleTypeConfig scaleType = ReadScaleType(ref reader);
            scaleTypes.Add(scaleType);
        }

        return new ScaleTypeConfigs(scaleTypes);
    }

    private static ScaleTypeConfig ReadScaleType(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected an object");
        }

        string? name = null;
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
                case nameof(name):
                {
                    reader.Read();
                    name = reader.GetString();
                    break;
                }
                case nameof(scales):
                {
                    reader.Read();
                    scales = ReadScales(ref reader);
                    break;
                }
                default:
                {
                    throw new JsonException($"Invalid property: {propertyName}");
                }
            }
        }

        if (name == null)
        {
            throw new JsonException($"Property '{nameof(name)}' is missing");
        }

        if (!CommonRegexes.ValidCodeSymbolRegex.IsMatch(name))
        {
            throw new JsonException($"Invalid {nameof(name)} '{name}'");
        }

        if (scales == null)
        {
            throw new JsonException($"Property '{nameof(scales)}' is missing");
        }

        return new ScaleTypeConfig(name, scales);
    }

    internal static IReadOnlyList<ScaleConfig> ReadScales(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected an array");
        }

        var scales = new List<ScaleConfig>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            ScaleConfig scale = ReadScale(ref reader);
            scales.Add(scale);
        }

        return scales;
    }

    private static ScaleConfig ReadScale(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected an object");
        }

        string? id = null;
        string? label = null;
        string? unit = null;
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
                case nameof(label):
                {
                    reader.Read();
                    label = reader.GetString();
                    break;
                }
                case nameof(unit):
                {
                    reader.Read();
                    unit = reader.GetString();
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

        if (label == null)
        {
            throw new JsonException($"Property '{nameof(label)}' is missing");
        }

        if (!CommonRegexes.ValidStringRegex.IsMatch(label))
        {
            throw new JsonException($"Invalid {nameof(label)} '{label}'");
        }

        if (unit != null && !CommonRegexes.ValidStringRegex.IsMatch(unit))
        {
            throw new JsonException($"Invalid {nameof(unit)} '{unit}'");
        }

        return new ScaleConfig(id, label, unit);
    }
}

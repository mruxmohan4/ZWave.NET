namespace ZWave.BuildTools;

public sealed class SensorTypeConfig
{
    public SensorTypeConfig(string id, string name, string displayName, string? scaleType, IReadOnlyList<ScaleConfig>? scales)
    {
        Id = id;
        Name = name;
        DisplayName = displayName;
        ScaleType = scaleType;
        Scales = scales;
    }

    public string Id { get; }

    public string Name { get; }

    public string DisplayName { get; }

    public string? ScaleType { get; }

    public IReadOnlyList<ScaleConfig>? Scales { get; }
}

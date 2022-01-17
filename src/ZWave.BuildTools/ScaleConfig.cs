namespace ZWave.BuildTools;

public sealed class ScaleConfig
{
    public ScaleConfig(string id, string label, string? unit)
    {
        Id = id;
        Label = label;
        Unit = unit;
    }

    public string Id { get; }

    public string Label { get; }

    public string? Unit { get; }
}

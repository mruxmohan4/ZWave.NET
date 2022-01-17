namespace ZWave.BuildTools;

public sealed class ScaleTypeConfig
{
    public ScaleTypeConfig(string name, IReadOnlyList<ScaleConfig> scales)
    {
        Name = name;
        Scales = scales;
    }

    public string Name { get; }

    public IReadOnlyList<ScaleConfig> Scales { get; }
}

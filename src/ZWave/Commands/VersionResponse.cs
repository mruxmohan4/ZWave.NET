using System.Text;

using ZWave.Serial;

namespace ZWave.Commands;

internal struct VersionResponse : ICommand<VersionResponse>
{
    public VersionResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.Version;

    public DataFrame Frame { get; }

    /// <summary>
    /// The Z-Wave API library version that runs on the Z-Wave Module
    /// </summary>
    public string LibraryVersion
    {
        get
        {
            // This is a null-terminated ASCII string
            ReadOnlySpan<byte> bytes = Frame.CommandParameters.Span[0..12];

            var nullIndex = bytes.IndexOf((byte)0);
            if (nullIndex != -1)
            {
                bytes = bytes[..nullIndex];
            }

            return Encoding.ASCII.GetString(bytes);
        }
    }

    /// <summary>
    /// The library type that runs on the Z-Wave Module.
    /// </summary>
    public VersionLibraryType LibraryType => (VersionLibraryType)Frame.CommandParameters.Span[12];

    public static VersionResponse Create(DataFrame frame) => new VersionResponse(frame);
}

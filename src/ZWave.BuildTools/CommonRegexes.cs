using System.Text.RegularExpressions;

namespace ZWave.BuildTools;

internal static class CommonRegexes
{
    internal static readonly Regex ValidHexRegex = new Regex(@"^0x[\da-f][\da-f]$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    internal static readonly Regex ValidCodeSymbolRegex = new Regex(@"^[A-Z][A-Za-z0-9_]+$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    internal static readonly Regex ValidStringRegex = new Regex(@"^[^""]+$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
}

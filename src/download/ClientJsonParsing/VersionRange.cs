using System;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class VersionRange
{
    #nullable enable
    [JsonPropertyName("min")]
    public string? Min;

    [JsonPropertyName("max")]
    public string? Max;
}

public static class SemanticVersionParser
{
    public static bool Parse(VersionRange versionRange, Version version)
    {
        if (versionRange.Min != null && versionRange.Max == null)
        {
            Version lowBound = Version.Parse(versionRange.Min);
            return version > lowBound;
        }
        if (versionRange.Min == null && versionRange.Max != null)
        {
            Version highBound = Version.Parse(versionRange.Max);
            return version < highBound;
        }
        if (versionRange.Min != null && versionRange.Max != null)
        {
            Version lowBound = Version.Parse(versionRange.Min);
            Version highBound = Version.Parse(versionRange.Max);
            return (lowBound < version) && (version < highBound);
        }
        return false; // defaulting to true might be slightly more fail-safe? incorrect jvm args usually shouldn't be a major issue, just a perf loss.
    }
}
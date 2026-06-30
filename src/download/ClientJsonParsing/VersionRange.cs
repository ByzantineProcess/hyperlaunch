using System;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class VersionRange
{
    #nullable enable
    [JsonPropertyName("min")]
    public string? Min { get; set; }

    [JsonPropertyName("max")]
    public string? Max { get; set; }
}

public static class SemanticVersionParser
{
    public static bool Parse(VersionRange versionRange, Version version)
    {
        if (versionRange.Min != null && versionRange.Max == null)
        {
            Version lowBound = Version.Parse(versionRange.Min);
            // Console.WriteLine($"low bound parsed as {lowBound}, cur version is {version}, version considered greater: {version >= lowBound}");
            return version >= lowBound;
        }
        if (versionRange.Min == null && versionRange.Max != null)
        {
            Version highBound = Version.Parse(versionRange.Max);
            // Console.WriteLine($"high bound parsed as {highBound}, cur version is {version}, version considered lesser: {version <= highBound}");
            return version <= highBound;
        }
        if (versionRange.Min != null && versionRange.Max != null)
        {
            Version lowBound = Version.Parse(versionRange.Min);
            Version highBound = Version.Parse(versionRange.Max);
            return (lowBound < version) && (version < highBound);
        }
        Console.WriteLine($"the following version check is false because apparently everything was null, {versionRange.Min}, {versionRange.Max}");
        return false; // defaulting to true might be slightly more fail-safe? incorrect jvm args usually shouldn't be a major issue, just a perf loss.
    }
}
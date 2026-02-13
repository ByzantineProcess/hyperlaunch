using Hyperlaunch;

namespace Hyperlaunch.Download;

public class GameVersion
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Time { get; set; } = "";
    public string ReleaseTime { get; set; } = "";
    public string Url { get; set; } = "";
    public string Sha1 { get; set; } = "";
    public int ComplianceLevel { get; set; } = 0;
}
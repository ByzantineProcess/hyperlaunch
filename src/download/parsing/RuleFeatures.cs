using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class RuleFeatures
{
    [JsonPropertyName("is_demo_user")]
    public bool? IsDemoUser { get; set; }

    [JsonPropertyName("has_custom_resolution")]
    public bool? HasCustomResolution { get; set; }

    [JsonPropertyName("has_quick_plays_support")]
    public bool? HasQuickPlaysSupport { get; set; }

    [JsonPropertyName("is_quick_play_singleplayer")]
    public bool? IsQuickPlaySingleplayer { get; set; }

    [JsonPropertyName("is_quick_play_multiplayer")]
    public bool? IsQuickPlayMultiplayer { get; set; }

    [JsonPropertyName("is_quick_play_realms")]
    public bool? IsQuickPlayRealms { get; set; }
}
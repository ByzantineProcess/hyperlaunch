
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Hyperlaunch.Download;

namespace Hyperlaunch.Instances.Loaders;

public class NeoForgeInstallProfile
{
    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; }
}
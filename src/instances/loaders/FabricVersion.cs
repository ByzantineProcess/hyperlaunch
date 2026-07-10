using System.Text.Json.Serialization;

namespace Hyperlaunch.Instances.Loaders;

public class FabricVersion
{
    [JsonPropertyName("loader")]
    public Loader Loader {get; set;}

    [JsonPropertyName("intermediary")]
    public Intermediary Intermediary {get; set;}
}

public class Loader
{
    [JsonPropertyName("separator")]
    public string Separator  {get; set;}

    [JsonPropertyName("build")]
    public int Build  {get; set;}

    [JsonPropertyName("maven")]
    public string Maven {get; set;}

    [JsonPropertyName("version")]
    public string Version {get; set;}

    [JsonPropertyName("stable")]
    public bool Stable {get; set;}
}

public class Intermediary
{
    [JsonPropertyName("maven")]
    public string Maven {get; set;}

    [JsonPropertyName("version")]
    public string Version {get; set;}

    [JsonPropertyName("stable")]
    public bool Stable {get; set;}
}
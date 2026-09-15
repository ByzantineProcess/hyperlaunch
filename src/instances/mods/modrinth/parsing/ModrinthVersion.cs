
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Instances.Mods.Modrinth;

// V3 has some extensions to this that don't seem to matter
public class ModrinthVersion
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("version_number")]
    public string VersionNumber { get; set; }

    [JsonPropertyName("changelog")]
    public string Changelog { get; set; }

    [JsonPropertyName("dependencies")]
    public List<Dependency> Dependencies  { get; set; }

    [JsonPropertyName("game_versions")]
    public List<string> GameVersions { get; set; }

    [JsonPropertyName("version_type")]
    public string VersionType { get; set; } // enum

    [JsonPropertyName("loaders")]
    public List<string> Loaders { get; set; }

    [JsonPropertyName("featured")]
    public bool Featured { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } // enum

    [JsonPropertyName("requested_status")]
    public string RequestedStatus { get; set; } // probably null, enum

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; }

    [JsonPropertyName("author_id")]
    public string AuthorId { get; set; }

    [JsonPropertyName("date_published")]
    public string PublishedDate { get; set; }

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("environment")]
    public string Environment { get; set; }

    [JsonPropertyName("files")]
    public List<VersionFile> VersionFiles { get; set; }

    public override string ToString()
    {
        return PrettyToString.Generic(this);
    }
}
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Instances.Mods.Modrinth;

#nullable enable

public class SearchResponse
{
    [JsonPropertyName("hits")]
    public required List<Project> Hits { get; set; }


    [JsonPropertyName("offset")]
    public required int Offset { get; set; }

    [JsonPropertyName("limit")]
    public required int Limit { get; set; }

    [JsonPropertyName("total_hits")]
    public required int TotalHits { get; set; }
}
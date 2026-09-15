using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Instances.Mods.Modrinth;

#nullable enable

public class SearchResponse
{
    [JsonPropertyName("hits")]
    public required List<MiniProject> Hits { get; set; }


    [JsonPropertyName("page")]
    public required int Page { get; set; }

    [JsonPropertyName("hits_per_page")]
    public required int HitsPerPage { get; set; }

    [JsonPropertyName("total_hits")]
    public required int TotalHits { get; set; }
}

public class SearchResponseV2
{
    [JsonPropertyName("hits")]
    public required List<V2Project> Hits { get; set; }

    [JsonPropertyName("offset")]
    public required int Offset { get; set; }

    [JsonPropertyName("limit")]
    public required int Limit { get; set; }

    [JsonPropertyName("total_hits")]
    public required int TotalHits { get; set; }
}
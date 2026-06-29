using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class LibraryDownloads
{
    #nullable enable
    [JsonPropertyName("artifact")]
    public LibraryArtifact? Artifact { get; set; }

    [JsonPropertyName("classifiers")]
    public Dictionary<string, LibraryArtifact>? Classifiers { get; set; }
    #nullable disable
}
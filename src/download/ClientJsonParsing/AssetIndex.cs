using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class AssetIndex
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("totalSize")]
    public int TotalSize { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    public AssetManifest Index { get; private set; }

    public async Task LoadIndexAsync()
    {
        Index = await AssetManifest.SmartLoadAsync(Url, Sha1, Id);
    }
}
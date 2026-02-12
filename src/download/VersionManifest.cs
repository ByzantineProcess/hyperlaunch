using System.Net.Http;
using System.Threading.Tasks;

namespace Hyperlaunch.Download;

public static class VersionManifest
{
    public static async Task<GameVersion> GetLatestAsync()
    {
        HttpResponseMessage response = await Http.Client.GetAsync("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
    }
}
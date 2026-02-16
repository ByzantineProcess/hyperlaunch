using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hyperlaunch;

public class GameAccount
{
    // TODO: encrypt in memory?
    public string MinecraftAccessToken { get; set; }
    [JsonPropertyName("name")]
    public string MinecraftUsername { get; init; }
    [JsonPropertyName("id")]
    public string MinecraftUUID { get; init; }
    public Skin CurrentSkin { get; set; }

    #nullable enable
    public Cape? CurrentCape { get; set; }
    #nullable disable
    
    [JsonPropertyName("skins")]
    public Skin[] Skins { get; init; }
    [JsonPropertyName("capes")]
    public Cape[] Capes { get; init; }
    public static async Task<GameAccount> FetchProfileAsync(string mcAccessToken)
    {
        var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
        profileRequest.Headers.Add("Authorization", "Bearer " + mcAccessToken);
        
        var profileResponse = await Http.Client.SendAsync(profileRequest);
        profileResponse.EnsureSuccessStatusCode();

        Log.Print("Successfully fetched Minecraft profile data. Response: " + await profileResponse.Content.ReadAsStringAsync());

        return await profileResponse.Content.ReadFromJsonAsync<GameAccount>();
    }
    public static async Task<GameAccount> CreateAsync(string msAccessToken)
    {
        string mcAccessToken = await MinecraftServices.ExchangeTokens(msAccessToken);
        var profileData = await FetchProfileAsync(mcAccessToken);
        Log.Print("Successfully fetched Minecraft profile data for " + profileData.MinecraftUsername);
        profileData.MinecraftAccessToken = mcAccessToken;
        return profileData;
    }
}

public class Skin
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("state")]
    public string State { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("variant")]
    public string Variant { get; set; }
    [JsonPropertyName("alias")]
    public string Alias { get; set; }
}

public class Cape
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("state")]
    public string State { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("alias")]
    public string Alias { get; set; }
}

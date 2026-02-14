using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Hyperlaunch;

public class GameAccount
{
    // TODO: encrypt in memory?
    public string MinecraftAccessToken { get; private set; }
    public string MinecraftUsername { get; private set; }
    public string MinecraftUUID { get; private set; }
    public Skin CurrentSkin { get; private set; }

    #nullable enable
    public Cape? CurrentCape { get; private set; }
    #nullable disable
    
    public Skin[] Skins { get; private set; }
    public Cape[] Capes { get; private set; }


    public GameAccount(string mcAccessToken)
    {
        MinecraftAccessToken = mcAccessToken;
    }

    public static async Task FetchProfileAsync(string mcAccessToken)
    {
        var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
        profileRequest.Headers.Add("Authorization", "Bearer " + mcAccessToken);
        
        var profileResponse = await Http.Client.SendAsync(profileRequest);
        profileResponse.EnsureSuccessStatusCode();
        
        
    }
    public static async Task<GameAccount> CreateAsync(string msAccessToken)
    {
        string mcAccessToken = await MinecraftServices.ExchangeTokens(msAccessToken);
        return new GameAccount(mcAccessToken);
    }
}

public class Skin
{
    public string Id { get; set; }
    public string State { get; set; }
    public string Url { get; set; }
    public string Variant { get; set; }
    public string Alias { get; set; }
}

public class Cape
{
    public string Id { get; set; }
    public string State { get; set; }
    public string Url { get; set; }
    public string Alias { get; set; }
}

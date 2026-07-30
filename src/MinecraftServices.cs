using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hyperlaunch;

public static class MinecraftServices
{
    // despite the name this is just for getting tokens
    // everything specific to one account is handled by GameAccount.cs
    public static async Task<string> ExchangeTokens(string msAccessToken)
    {
        XboxLiveAuthRequest xboxLiveRequestBody = new XboxLiveAuthRequest
        {
            Properties = new XboxLiveAuthProperties
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={msAccessToken}"
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };
        var xboxLiveResponse = await Http.PostAsJsonAsync("https://user.auth.xboxlive.com/user/authenticate", xboxLiveRequestBody, HyperlaunchJsonContext.Default.XboxLiveAuthRequest);
        try
        {
            xboxLiveResponse.EnsureSuccessStatusCode();
        }
        catch
        {
            Log.Print("Xbox Live authentication failed. Response: " + await xboxLiveResponse.Content.ReadAsStringAsync());
        }
        // GD.Print(await xboxLiveResponse.Content.ReadAsStringAsync());
        using var xboxLiveDocument = await JsonDocument.ParseAsync(await xboxLiveResponse.Content.ReadAsStreamAsync());
        var xboxLiveData = xboxLiveDocument.RootElement;
        string xboxLiveToken = xboxLiveData.GetProperty("Token").GetString();
        string userHash = xboxLiveData
            .GetProperty("DisplayClaims")
            .GetProperty("xui")[0]
            .GetProperty("uhs")
            .GetString();


        var xstsRequestBody = new XstsAuthRequest
        {
            Properties = new XstsAuthProperties
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xboxLiveToken }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };
        var xstsResponse = await Http.PostAsJsonAsync("https://xsts.auth.xboxlive.com/xsts/authorize", xstsRequestBody, HyperlaunchJsonContext.Default.XstsAuthRequest);
        if (xstsResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            using var xstsErrorDocument = await JsonDocument.ParseAsync(await xstsResponse.Content.ReadAsStreamAsync());
            var xstsErrorData = xstsErrorDocument.RootElement;
            uint xstsErrorCode = xstsErrorData.GetProperty("XErr").GetUInt32(); // freaky c# uint weirdness
            switch (xstsErrorCode)
            {
                case 2148916227:
                   throw new System.Exception("This account is banned from Xbox.");
                
                case 2148916233:
                    throw new System.Exception("This account doesn't have an Xbox profile. You probably don't own Minecraft on this account.");
                
                case 2148916235:
                    throw new System.Exception("Xbox Live is banned in this country/region.");
                
                case 2148916236 or 2148916237:
                    throw new System.Exception("The account needs adult verification on Xbox. (South Korea)");
                
                case 2148916238:
                    throw new System.Exception("The account is a child account and needs parental consent to access Xbox Live.");

                default:
                    throw new System.Exception("XSTS authorization failed with error code: " + xstsErrorCode);
            }
        }
        xstsResponse.EnsureSuccessStatusCode();
        // GD.Print(await xstsResponse.Content.ReadAsStringAsync());
        using var xstsDocument = await JsonDocument.ParseAsync(await xstsResponse.Content.ReadAsStreamAsync());
        var xstsData = xstsDocument.RootElement;
        string xstsToken = xstsData.GetProperty("Token").GetString();

        if (!(xstsData.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString() == userHash))
        {
            throw new System.Exception("User hash mismatch between Xbox Live and XSTS tokens.");
        }

        var mcLoginRequestBody = new MinecraftLoginRequest
        {
            IdentityToken = $"XBL3.0 x={userHash};{xstsToken}"
        };
        var mcLoginResponse = await Http.PostAsJsonAsync("https://api.minecraftservices.com/authentication/login_with_xbox", mcLoginRequestBody, HyperlaunchJsonContext.Default.MinecraftLoginRequest);
        mcLoginResponse.EnsureSuccessStatusCode();
        // GD.Print(await mcLoginResponse.Content.ReadAsStringAsync());
        using var mcLoginDocument = await JsonDocument.ParseAsync(await mcLoginResponse.Content.ReadAsStreamAsync());
        var mcLoginData = mcLoginDocument.RootElement;
        string mcAccessToken = mcLoginData.GetProperty("access_token").GetString();

        return mcAccessToken;
    }
}

public class XboxLiveAuthRequest
{
    public XboxLiveAuthProperties Properties { get; set; }
    public string RelyingParty { get; set; }
    public string TokenType { get; set; }
}

public class XboxLiveAuthProperties
{
    public string AuthMethod { get; set; }
    public string SiteName { get; set; }
    public string RpsTicket { get; set; }
}

public class XstsAuthRequest
{
    public XstsAuthProperties Properties { get; set; }
    public string RelyingParty { get; set; }
    public string TokenType { get; set; }
}

public class XstsAuthProperties
{
    public string SandboxId { get; set; }
    public string[] UserTokens { get; set; }
}

public class MinecraftLoginRequest
{
    [JsonPropertyName("identityToken")]
    public string IdentityToken { get; set; }
}
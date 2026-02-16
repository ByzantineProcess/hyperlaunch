using System.Net.Http.Json;
using System.Threading.Tasks;
using Godot;

namespace Hyperlaunch;

public static class MinecraftServices
{
    // despite the name this is just for getting tokens
    // everything specific to one account is handled by GameProfile.cs
    public static async Task<string> ExchangeTokens(string msAccessToken)
    {
        // TODO: add trycatches to literally all of this 
        var xboxLiveRequestBody = new
        {
            Properties = new
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={msAccessToken}"
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };
        var xboxLiveResponse = await Http.PostAsJsonAsync("https://user.auth.xboxlive.com/user/authenticate", xboxLiveRequestBody);
        try
        {
            xboxLiveResponse.EnsureSuccessStatusCode();
        }
        catch
        {
            GD.Print("Xbox Live authentication failed. Response: " + await xboxLiveResponse.Content.ReadAsStringAsync());
        }
        // GD.Print(await xboxLiveResponse.Content.ReadAsStringAsync());
        var xboxLiveData = await xboxLiveResponse.Content.ReadFromJsonAsync<dynamic>();
        string xboxLiveToken = xboxLiveData.GetProperty("Token").GetString();
        string userHash = xboxLiveData
            .GetProperty("DisplayClaims")
            .GetProperty("xui")[0]
            .GetProperty("uhs")
            .GetString();


        var xstsRequestBody = new
        {
            Properties = new
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xboxLiveToken }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };
        var xstsResponse = await Http.PostAsJsonAsync("https://xsts.auth.xboxlive.com/xsts/authorize", xstsRequestBody);
        if (xstsResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var xstsErrorData = await xstsResponse.Content.ReadFromJsonAsync<dynamic>();
            uint xstsErrorCode = xstsErrorData.GetProperty("XErr").GetUInt32(); // freaky c# uint weirdness
            switch (xstsErrorCode)
            {
                case 2148916227:
                   throw new System.Exception("This account is banned from Xbox.");
                
                case 2148916233:
                    throw new System.Exception("This account doesn't have an Xbox profile. This is very odd if you own Minecraft on this account.");
                
                case 2148916235:
                    throw new System.Exception("Xbox Live is banned in this country/region.");
                
                case 2148916236 or 2148916237:
                    throw new System.Exception("The account needs adult verification on Xbox page. (South Korea)");
                
                case 2148916238:
                    throw new System.Exception("The account is a child account and needs parental consent to access Xbox Live.");

                default:
                    throw new System.Exception("XSTS authorization failed with error code: " + xstsErrorCode);
            }
        }
        xstsResponse.EnsureSuccessStatusCode();
        // GD.Print(await xstsResponse.Content.ReadAsStringAsync());
        var xstsData = await xstsResponse.Content.ReadFromJsonAsync<dynamic>();
        string xstsToken = xstsData.GetProperty("Token").GetString();

        if (!(xstsData.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString() == userHash))
        {
            throw new System.Exception("User hash mismatch between Xbox Live and XSTS tokens.");
        }

        var mcLoginRequestBody = new
        {
            identityToken = $"XBL3.0 x={userHash};{xstsToken}"
        };
        var mcLoginResponse = await Http.PostAsJsonAsync("https://api.minecraftservices.com/authentication/login_with_xbox", mcLoginRequestBody);
        mcLoginResponse.EnsureSuccessStatusCode();
        // GD.Print(await mcLoginResponse.Content.ReadAsStringAsync());
        var mcLoginData = await mcLoginResponse.Content.ReadFromJsonAsync<dynamic>();
        string mcAccessToken = mcLoginData.GetProperty("access_token").GetString();

        return mcAccessToken;
    }
}
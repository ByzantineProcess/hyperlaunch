using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Godot;
using System.Linq;

namespace Hyperlaunch;

public static class MSAuth
{
    private static IPublicClientApplication app = PublicClientApplicationBuilder
        .Create("c36a9fb6-4f2a-41ff-90bd-ae7cc92031eb")
        .WithAuthority("https://login.microsoftonline.com/consumers")
        .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
        .Build();

    public static async Task<string> Login()
    {

        var accounts = await app.GetAccountsAsync();
        AuthenticationResult result = null;
        try
        {
            result = await app.AcquireTokenSilent(["XboxLive.signin"], accounts.FirstOrDefault()).ExecuteAsync();
        } catch (MsalUiRequiredException)
        {
            return await AuthenticateAsync();
        }
        return result.AccessToken;
    }
    
    public static async Task<string> AuthenticateAsync()
    {
        var result = await app.AcquireTokenWithDeviceCode(["XboxLive.signin"], deviceCodeResult =>
        {
            GD.Print(deviceCodeResult.UserCode);
            return Task.FromResult(0);
        }).ExecuteAsync();
        return result.AccessToken;
    }
}
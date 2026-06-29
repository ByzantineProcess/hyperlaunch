using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Hyperlaunch;

public static class MSAuth
{
    private static IPublicClientApplication app = PublicClientApplicationBuilder
        .Create("25c2eb21-47d5-4262-84c5-a308c11ee76a")
        .WithAuthority("https://login.microsoftonline.com/consumers")
        .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
        .Build();

    public static async Task<string> Login()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var cacheHelper = await MsalCacheHelper.CreateAsync(new StorageCreationPropertiesBuilder("msal_cache.dat", homeDirectory + "/.hyperlaunch")
            .WithMacKeyChain("HyperlaunchMSALCache", "Hyperlaunch")
            .WithLinuxKeyring(
                "hyperlaunch",
                MsalCacheHelper.LinuxKeyRingDefaultCollection,
                "hyperlaunch",
                new KeyValuePair<string, string>("Version", "0.1"), // according to msal docs changing version invalidates older cache
                new KeyValuePair<string, string>("Product", "Hyperlaunch"))
            .Build());
        
        cacheHelper.RegisterCache(app.UserTokenCache);

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
        // TODO: gui
        var result = await app.AcquireTokenWithDeviceCode(["XboxLive.signin"], deviceCodeResult =>
        {
            Log.Print($"To authenticate, visit {deviceCodeResult.VerificationUrl} and enter the code: {deviceCodeResult.UserCode}");
            return Task.FromResult(0);
        }).ExecuteAsync();
        return result.AccessToken;
    }
}
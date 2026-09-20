using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

namespace Hyperlaunch;

public static class MSAuth
{
    private static IPublicClientApplication app = PublicClientApplicationBuilder
        .Create("25c2eb21-47d5-4262-84c5-a308c11ee76a") // please do not use in your own projects, getting your own is a lot easier than you think.
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
        AuthenticationResult result;
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
        AuthenticationResult result = await app.AcquireTokenWithDeviceCode(["XboxLive.signin"], deviceCodeResult =>
        {
            Log.Print($"To authenticate, visit {deviceCodeResult.VerificationUrl} and enter the code: {deviceCodeResult.UserCode}");
            return Task.FromResult(0);
        }).ExecuteAsync();
        return result.AccessToken;
    }

    #nullable enable
    #if GODOT

    public static void NonBlockingAuthenticate(Func<DeviceCodeResult, Task> func, Func<GameAccount, int> callback)
    {
        AuthenticationResult result;
        GameAccount account;
        Task.Run(async () =>
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
                try
                {
                    result = await app.AcquireTokenSilent(["XboxLive.signin"], accounts.FirstOrDefault()).ExecuteAsync();
                    account = await GameAccount.CreateAsync(result.AccessToken);
                    callback.Invoke(account);
                } catch (MsalUiRequiredException)
                {
                    AuthenticationResult result = await app.AcquireTokenWithDeviceCode(["XboxLive.signin"], func).ExecuteAsync();
                    account = await GameAccount.CreateAsync(result.AccessToken);
                    callback.Invoke(account);
                }
            }
        );
    }

    #endif
}
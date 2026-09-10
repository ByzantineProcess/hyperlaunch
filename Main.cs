using Godot;
using Hyperlaunch.Download;
using Hyperlaunch.Instances;
using Hyperlaunch.Launch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hyperlaunch.GodotGui;

public partial class Main : Control
{
    GameAccount account;
    public override async void _Ready()
    {
        string appData  = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        string localApp = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        Directory.CreateDirectory(Path.Combine(appData,  ".hyperlaunch/"));
        Directory.CreateDirectory(Path.Combine(localApp, ".hyperlaunch/"));
        Directory.CreateDirectory(Path.Combine(localApp, ".hyperlaunch/", "instances/"));

        await Settings.SettingsContainer.LoadAsync(
            Path.Combine(appData, ".hyperlaunch/settings.json")); 
        
        await VersionManifest.LoadAsync();
        GetNode<RichTextLabel>("Latest").Text = "Latest Minecraft release: " + VersionManifest.Data.Latest.Release;
    }
    public async void _on_login_button_pressed()
    {
        string msToken = await MSAuth.Login();
        account = await GameAccount.CreateAsync(msToken);
    }

    public async void _on_launch_button_pressed()
    {
        // attempt to launch the game (?)
        // this button is bound to Launch Latest in the testgrounds.tscn
        await LaunchMinecraft.AuthAndLaunch(VersionManifest.GetLatestRelease().Id, Instance.Default());
    }

    public async void _on_yolo_pressed()
    {
        // get the Interactibles/LineEdit node
        LineEdit lineEdit = GetNode<LineEdit>("interactibles/LineEdit");
        string versionId = lineEdit.Text;

        await HandleLaunch(versionId, false);
    }

    #nullable enable

    static async Task HandleLaunch(string versionId, bool forceModernJava, ClientManifest? overrideClientManifest = null)
    {
        Log.Print("starting launch");

        if (overrideClientManifest is not null)
        {
            await LaunchMinecraft.AuthAndLaunch(overrideClientManifest, Instance.Default());
        }
        else
        {
            await LaunchMinecraft.AuthAndLaunch(versionId, Instance.Default());
        }
    }
}

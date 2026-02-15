using Godot;
using Hyperlaunch.Download;
using Hyperlaunch.Launch;
using System;
using System.Collections.Generic;

namespace Hyperlaunch.GodotGui;

public partial class Main : Control
{
    GameAccount account;
    public override async void _Ready()
    {
        GD.Print("Main scene ready");
        await Settings.SettingsContainer.LoadAsync(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "/.hyperlaunch/settings.json");
        GD.Print("Settings loaded");
        GD.Print(Settings.SettingsContainer.Current.SaveToString());
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
        Jvm jvm = new Jvm("java"); // this will need to be changed to the actual java path later
        GameVersion latestVersion = VersionManifest.GetLatestRelease();
        ClientManifest clientManifest = await ClientManifest.LoadFromUrlAsync(latestVersion.Url);
        List<DownloadTask> downloadTasks = DownloadTask.FromClientJson(clientManifest);

        var progress = new Progress<long>(bytesRemaining =>
        {
            double mbRemaining = bytesRemaining / (1024.0 * 1024.0);
            GD.Print($"Download progress: {mbRemaining:F2} MB remaining");
        });

        await DownloadTask.ExecuteAllAsync(downloadTasks, maxConcurrentNetwork: 4, bytesRemainingProgress: progress);

        LaunchMinecraft.Launch(jvm, account, clientManifest);
    }
}

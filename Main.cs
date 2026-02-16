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
        // ensure both .hyperlaunch folders exist in both local and roaming
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), ".hyperlaunch/"));
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
        Jvm jvm = new Jvm("javaw"); // this will need to be changed to the actual java path later
        GameVersion latestVersion = VersionManifest.GetLatestRelease();
        ClientManifest clientManifest = await ClientManifest.LoadFromUrlAsync(latestVersion.Url);
        clientManifest.AssetIndex.Index.SaveInCorrectSpot();
        List<DownloadTask> downloadTasks = DownloadTask.FromClientJson(clientManifest);

        var progress = new Progress<long>(bytesRemaining =>
        {
            double mbRemaining = bytesRemaining / (1024.0 * 1024.0);
            GD.Print($"Download progress: {mbRemaining:F2} MB remaining");
        });

        await DownloadTask.ExecuteAllAsync(downloadTasks, maxConcurrentNetwork: 20, bytesRemainingProgress: progress);

        string nativesDir = System.IO.Path.Combine(DownloadTask.BasePath, "natives/", clientManifest.Id);
        DownloadTask.ExtractNatives(clientManifest, nativesDir);

        LaunchMinecraft.Launch(jvm, account, clientManifest);
    }

    public async void _on_yolo_pressed()
    {
        // get the Interactibles/LineEdit node
        LineEdit lineEdit = GetNode<LineEdit>("interactibles/LineEdit");
        string versionId = lineEdit.Text;
        GameVersion version = VersionManifest.GetVersionById(versionId);
        ClientManifest clientManifest = await ClientManifest.LoadFromUrlAsync(version.Url);
        clientManifest.AssetIndex.Index.SaveInCorrectSpot();
        List<DownloadTask> downloadTasks = DownloadTask.FromClientJson(clientManifest);
        var progress = new Progress<long>(bytesRemaining =>
        {
            double mbRemaining = bytesRemaining / (1024.0 * 1024.0);
            GD.Print($"Download progress: {mbRemaining:F2} MB remaining");
        });
        await DownloadTask.ExecuteAllAsync(downloadTasks, maxConcurrentNetwork: 50, bytesRemainingProgress: progress);

        string nativesDir = System.IO.Path.Combine(DownloadTask.BasePath, "natives/", clientManifest.Id);
        DownloadTask.ExtractNatives(clientManifest, nativesDir);

        Jvm jvm = new Jvm("javaw");
        LaunchMinecraft.Launch(jvm, account, clientManifest);
    }
}

using Godot;
using System;

namespace Hyperlaunch;

public partial class Main : Control
{
    public override async void _Ready()
    {
        GD.Print("Main scene ready");
        await Settings.SettingsContainer.LoadAsync(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "/.hyperlaunch/settings.json");
        GD.Print("Settings loaded");
        GD.Print(Settings.SettingsContainer.Current.SaveToString());
    }
    public async void _on_login_button_pressed()
    {
        string msToken = await MSAuth.Login();
        GD.Print("MS Access Token: " + msToken);
        var loginStartTime = DateTime.Now;
        string mcToken = await MinecraftServices.ExchangeTokens(msToken);
        GD.Print("Minecraft Access Token: " + mcToken);
        var loginEndTime = DateTime.Now;
        GD.Print("Login took " + (loginEndTime - loginStartTime).TotalSeconds + " seconds");
    }

    public async void _on_launch_button_pressed()
    {
        GD.Print("Launch button pressed");
        var jvms = Jvm.ScanForJvms();
        GD.Print("Found " + jvms.Count + " JVMs");
        foreach (var jvm in jvms)
        {
            GD.Print("JVM: " + jvm.ExecPath + ", version " + jvm.Version);
        }
    }
}

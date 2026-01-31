using Godot;
using System;

namespace Hyperlaunch;

public partial class Main : Control
{
    public async void _on_login_button_pressed()
    {
        // get time now
        var loginStartTime = DateTime.Now;
        string msToken = await MSAuth.Login();
        GD.Print("MS Access Token: " + msToken);
        string mcToken = await MinecraftServices.ExchangeTokens(msToken);
        GD.Print("Minecraft Access Token: " + mcToken);
        var loginEndTime = DateTime.Now;
        GD.Print("Login took " + (loginEndTime - loginStartTime).TotalSeconds + " seconds");
    }

    public void _on_launch_button_pressed()
    {
        GD.Print("Launch button pressed");
    }
}

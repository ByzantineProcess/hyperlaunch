using Godot;
using System;
using Hyperlaunch;
using System.IO;
using Hyperlaunch.Download;
using Hyperlaunch.Settings;
using System.Collections.Generic;
using System.Linq;
using Hyperlaunch.Instances;
using System.Threading.Tasks;
using Hyperlaunch.Launch;
using Hyperlaunch.Instances.Loaders;
using Hyperlaunch.Instances.Mods.Modrinth;
using Hyperlaunch.Utilities;
using Microsoft.Identity.Client;
using System.Diagnostics;
using System.Data;

public partial class MvpMain : Control
{
    SearchResponse search;

	public override async void _Ready()
    {
        string appData  = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        string localApp = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        Directory.CreateDirectory(Path.Combine(appData,  ".hyperlaunch/"));
        Directory.CreateDirectory(Path.Combine(localApp, ".hyperlaunch/"));
        Directory.CreateDirectory(Path.Combine(localApp, ".hyperlaunch/", "instances/"));

        await SettingsContainer.LoadAsync(
            Path.Combine(appData, ".hyperlaunch/settings.json"));
        
        await VersionManifest.LoadAsync();

        OptionButton iVersions = GetNode<OptionButton>("Main/Content/BuildInstance/Space/VBoxContainer/IVersion");
        List<GameVersion> allReleases = VersionManifest.Data.Versions.Where(version => version.Type == "release").ToList();
        foreach (GameVersion version in allReleases)
        {
            iVersions.AddItem(version.Id);
        }

        RefreshInstances();
    }

    public void _on_create_instance_button_pressed()
    {
        string name = GetNode<LineEdit>("Main/Content/BuildInstance/Space/VBoxContainer/IName").Text;
        OptionButton iVersion = GetNode<OptionButton>("Main/Content/BuildInstance/Space/VBoxContainer/IVersion");
        string version = iVersion.GetItemText(iVersion.Selected);
        int iLoader = GetNode<OptionButton>("Main/Content/BuildInstance/Space/VBoxContainer/ILoader").Selected;
        ClientType clientType = ClientType.Vanilla;
        if (iLoader == 2) { clientType = ClientType.Fabric; }
        if (iLoader == 3) { clientType = ClientType.NeoForge; }

        Instance instance = new Instance(name, clientType, version, false);
        instance.Save();

        if (iLoader == 2 || iLoader == 3) { instance.EnsureModsFolderExists(); }

        RefreshInstances();

        _on_cancel_button_pressed_buildinstance();
    }

    public void RefreshInstances()
    {
        OptionButton iInstances = GetNode<OptionButton>("Main/Content/LaunchInstance/Space/VBoxContainer/IInstance");
        OptionButton iInstances2 = GetNode<OptionButton>("Main/Content/AddToInstnace/Space/VBoxContainer/IInstance");
        for (int i = 0; i < iInstances.ItemCount; i++)
        {
            iInstances.RemoveItem(i);
            iInstances2.RemoveItem(i);
        }
        foreach (string instance in Instance.ListAll())
        {
            iInstances.AddItem(instance);
            iInstances2.AddItem(instance);
        }
    }

    public void SetCode(string code)
    {
        GetNode<RichTextLabel>("Main/Content/Login/Space/VBoxContainer/Code").Text = $"code is: {code}";
        GetNode<Button>("Main/Content/Login/Space/VBoxContainer/LoginButtons/CopyCode").Disabled = false;
    }

    public void FinishLoginFlow(string username)
    {
        GetNode<RichTextLabel>("Main/Content/Login/Space/VBoxContainer/Instructions").Text = $"logged in as\n\"{username}\"";
        GetNode<RichTextLabel>("Main/Content/Login/Space/VBoxContainer/Code").Text = $"code is: ";
        GetNode<Button>("Main/Content/Login/Space/VBoxContainer/Buttons/FinishFlow").Disabled = false;
        GetNode<Button>("Main/Content/Login/Space/VBoxContainer/LoginButtons/CopyCode").Disabled = true;
        _on_cancel_button_pressed_login();
    }

    public void StartLoginFlow()
    {
        MSAuth.NonBlockingAuthenticate(deviceCode =>
        {
            CallDeferred("SetCode", deviceCode.UserCode);
            return Task.FromResult(0);
        }, account =>
        {
            CallDeferred("FinishLoginFlow", account.MinecraftUsername);
            return 0;
        }
        );
    }

    public async void _on_search_button_pressed()
    {
        OptionButton iInstance = GetNode<OptionButton>("Main/Content/AddToInstnace/Space/VBoxContainer/IInstance");
        string searchTerm = GetNode<LineEdit>("Main/Content/AddToInstnace/Space/VBoxContainer/ISearchQuery").Text;
        Instance instanceAdd = Instance.Load(Instance.ListAll()[iInstance.Selected]);

        FilterCollection filters = 
            new Filter(FilterType.Versions, FilterOps.Is, instanceAdd.MainVersion)
            .And(new Filter(FilterType.Categories, FilterOps.Is, StringEnum.Retrieve(instanceAdd.ClientType)));
        SearchResponse searchResponse = await ModrinthV3.Search(searchTerm, filters, limit: 5);

        search = searchResponse;

        OptionButton iSelectedMod = GetNode<OptionButton>("Main/Content/AddToInstnace/Space/VBoxContainer/ISelectedMod");
        iSelectedMod.Clear();
        

        foreach (MiniProject hit in searchResponse.Hits)
        {
            iSelectedMod.AddItem(hit.Name);
            iSelectedMod.SetItemMetadata(iSelectedMod.ItemCount-1, hit.Id);
        }

        Button installButton = GetNode<Button>("Main/Content/AddToInstnace/Space/VBoxContainer/HBoxContainer/InstallButton");
        installButton.Disabled = false;
    }

    public void _on_install_button_pressed()
    {
        OptionButton iInstance = GetNode<OptionButton>("Main/Content/AddToInstnace/Space/VBoxContainer/IInstance");
        Instance instanceAdd = Instance.Load(Instance.ListAll()[iInstance.Selected]);

        Button installButton = GetNode<Button>("Main/Content/AddToInstnace/Space/VBoxContainer/HBoxContainer/InstallButton");
        installButton.Disabled = true;

        OptionButton iSelectedMod = GetNode<OptionButton>("Main/Content/AddToInstnace/Space/VBoxContainer/ISelectedMod");
        string selectedProjectId = iSelectedMod.GetItemMetadata(iSelectedMod.Selected).As<string>();

        Task.Run(() => ModrinthV3.DownloadMod(instanceAdd, selectedProjectId).Wait()).ContinueWith(task => CallDeferred("DoneInstallingMod"));
    }

    public void DoneInstallingMod()
    {
        GetNode<Button>("Main/Content/AddToInstnace/Space/VBoxContainer/HBoxContainer/InstallButton").Disabled = false;
        _on_cancel_button_pressed_addtoinstance();
    }

    public void _on_launch_instance_button_pressed()
    {
        OptionButton iInstance = GetNode<OptionButton>("Main/Content/LaunchInstance/Space/VBoxContainer/IInstance");
        string instance = iInstance.GetItemText(iInstance.Selected);
        Task.Run(() => HandleLaunch(Instance.Load(instance)));
        _on_cancel_button_pressed_launchinstance();
    }

    public void _on_open_link_pressed()
    {
        OsInfo osInfo = OsInfo.Detect(); // https://stackoverflow.com/a/43232486
        if (osInfo.Name == "windows") {Process.Start("explorer", "https://microsoft.com/link");}
        if (osInfo.Name == "osx") {Process.Start("open", "https://microsoft.com/link");}
        if (osInfo.Name == "linux") {Process.Start("xdg-open", "https://microsoft.com/link");}  
    }

    public void _on_copy_code_pressed()
    {
        string deviceCode = GetNode<RichTextLabel>("Main/Content/Login/Space/VBoxContainer/Code").Text.Replace("code is: ", "");
        DisplayServer.ClipboardSet(deviceCode);
    }

    public void _on_login_pressed()
    {
        GetNode<Control>("Main/Content/Glue").Visible = false;
        GetNode<Control>("Main/Content/Login").Visible = true;
        StartLoginFlow();
    }

    public void _on_cancel_button_pressed_login()
    {
        GetNode<Control>("Main/Content/Glue").Visible = true;
        GetNode<Control>("Main/Content/Login").Visible = false;
    }

    public void _on_create_instance_pressed()
    {
        GetNode<Control>("Main/Content/Glue").Visible = false;
        GetNode<Control>("Main/Content/BuildInstance").Visible = true;
    }

    public void _on_cancel_button_pressed_buildinstance()
    {
        GetNode<Control>("Main/Content/Glue").Visible = true;
        GetNode<Control>("Main/Content/BuildInstance").Visible = false;
    }

    public void _on_launch_instance_pressed()
    {
        GetNode<Control>("Main/Content/Glue").Visible = false;
        GetNode<Control>("Main/Content/LaunchInstance").Visible = true;
    }

    public void _on_cancel_button_pressed_launchinstance()
    {
        GetNode<Control>("Main/Content/Glue").Visible = true;
        GetNode<Control>("Main/Content/LaunchInstance").Visible = false;
    }

    public void _on_add_mod_to_instance_pressed()
    {
        GetNode<Control>("Main/Content/Glue").Visible = false;
        GetNode<Control>("Main/Content/AddToInstnace").Visible = true;
    }

    public void _on_cancel_button_pressed_addtoinstance()
    {
        GetNode<Control>("Main/Content/Glue").Visible = true;
        GetNode<Control>("Main/Content/AddToInstnace").Visible = false;
    }

    public void _on_exit_pressed()
    {
        GetTree().Quit();
    }

    public void _on_finish_flow_pressed()
    {
        GetNode<Button>("Main/Content/Login/Space/VBoxContainer/Buttons/FinishFlow").Disabled = true;
    }

    public void UpdateStatus(string status)
    {
        if (status == null) { return; }
        RichTextLabel statusLabel = GetNode<RichTextLabel>("Main/Border/ColorRect2/Status");
        status += "  ";
        statusLabel.Text = status;
    }

	public override void _Process(double delta)
    {
        UpdateStatus(MvpMainsVeryOwnNotThreadSafeStatusContainer.Status);
    }


    static async Task HandleLaunch(Instance instance)
    {
        switch (instance.ClientType)
        {
            case ClientType.Vanilla:
                await LaunchMinecraft.AuthAndLaunch(instance.MainVersion, instance);
                break;
            
            case ClientType.Fabric:
                await LaunchMinecraft.AuthAndLaunch(await Fabric.GetClientManifestWithStableFabricAsync(instance.MainVersion, true), instance);
                break;
            
            case ClientType.NeoForge:
                if (!instance.MainVersion.StartsWith("26")) { Log.PrintErr("Obfuscated NeoForge is not ready yet."); }
                string neoForgeVersion = await NeoForge.GetLatestNeoForgeVersionAsync(instance.MainVersion);
                Log.Print($"latest neoforge is {neoForgeVersion}");
                ClientManifest newManifest = await NeoForge.GetClientManifestAsync(neoForgeVersion);
                await LaunchMinecraft.DownloadEverything(newManifest);
                await NeoForge.InstallLoader(newManifest, neoForgeVersion);
                await LaunchMinecraft.AuthAndLaunch(newManifest, instance);

                break;
            
            default:
                Log.Print("Other loaders are in progress.");
                break;
        }
    }
}


public static class MvpMainsVeryOwnNotThreadSafeStatusContainer
{
    public static string Status { get; set; }
}

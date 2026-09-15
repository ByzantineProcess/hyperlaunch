#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hyperlaunch.Download;
using Hyperlaunch.Instances;
using Hyperlaunch.Instances.Loaders;
using Hyperlaunch.Instances.Mods.Modrinth;
using Hyperlaunch.Launch;
using Hyperlaunch.Utilities;
using Microsoft.Identity.Client;

namespace Hyperlaunch.Cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            PrintUsage();
            return;
        }
        bool forceModernJava = false;
        if (args.Contains("--force-modern-java"))
        {
            forceModernJava = true;
        }

        string command = args[0].ToLowerInvariant();
        switch (command)
        {
            case "launch":
                await HandleLaunch(args, forceModernJava);
                break;

            case "login":
                await HandleLogin();
                break;

            case "versions":
                await HandleListVersions();
                break;
            
            case "launch-fabric":
                // Log.Print("waiting for enter");
                // Console.ReadLine();
                await Init();
                await VersionManifest.LoadAsync();
                ClientManifest fabricManifest = await Fabric.GetClientManifestWithStableFabricAsync(args[1], true);
                await HandleLaunch([], false, fabricManifest);
                break;
            
            case "gen-cmd":
                await HandleGenCmd(args);
                break;
            
            case "instance":
                await Init();
                await VersionManifest.LoadAsync();
                if (args.Length == 1) { Log.Print("instance command requires further arguments"); break; }
                switch (args[1].ToLowerInvariant())
                {
                    case "create":
                        if (args.Length == 5)
                        {
                            Instance instanceCreate = new Instance(args[2], (ClientType)StringEnum.Match(args[3], typeof(ClientType))!, args[4], false);
                            instanceCreate.Save();
                            Log.Print("New instance made:");
                            Log.Print(PrettyToString.Generic(instanceCreate));
                        }
                        break;
                    
                    case "launch":
                        await HandleLaunch(Instance.Load(args[2]));
                        break;
                    
                    case "add":
                        Instance instanceAdd = Instance.Load(args[2]);
                        FilterCollection filters = 
                            new Filter(FilterType.Versions, FilterOps.Is, instanceAdd.MainVersion)
                            .And(new Filter(FilterType.Categories, FilterOps.Is, StringEnum.Retrieve(instanceAdd.ClientType)));
                        SearchResponse searchResponse = await ModrinthV3.Search(args[3], filters, limit: 5);

                        Log.Print("Search results:");
                        int count = 0;
                        foreach (string name in searchResponse.Hits.Select(hit => hit.Name))
                        {
                            count++;
                            Log.Print($" {count}) {name}");
                        }
                        Console.Write("Selection: ");
                        string? selString = Console.ReadLine();
                        int sel = Convert.ToInt32(selString);

                        Directory.CreateDirectory(Path.Combine(instanceAdd.GetInstancePath(), "mods/"));

                        await ModrinthV3.DownloadMod(instanceAdd, searchResponse.Hits[sel-1].Id);

                        Log.Print($"Successfully downloaded {searchResponse.Hits[sel-1].Name} to the instance {instanceAdd.Name}");

                        break;

                    case "help" or "?":
                        Log.Print("Instance commands:");
                        Log.Print("instance create {name} {loader} {version}");
                        Log.Print("instance launch {name}");
                        Log.Print("instance add {name} {search query}");
                        break;

                    default:
                        Log.Print("Unknown instance command.");
                        break;
                }

                break;
            
            case "jarona":
                // super silly bypassing
                Log.Print($"performing sillyness with IP {Dns.GetHostAddresses("piston-meta.mojang.com")[0]}");
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, $"http://{Dns.GetHostAddresses("piston-meta.mojang.com")[0]}/mc/game/version_manifest_v2.json");
                message.Headers.TryAddWithoutValidation("Host", "piston-meta.mojang.com");
                HttpResponseMessage responseMessage = await Http.Client.SendAsync(message);
                responseMessage.EnsureSuccessStatusCode();
                Log.Print(await responseMessage.Content.ReadAsStringAsync());
                break;
            
            case "sustingus":
                await VersionManifest.LoadAsync();

                FilterCollection searchQuery = 
                    new Filter(FilterType.Versions, FilterOps.Is, "26.1")
                    .And(new Filter(FilterType.Categories, FilterOps.Is, "neoforge"));

                FilterCollection searchQueryPt2 = 
                    new Filter(FilterType.Versions, FilterOps.Is, "1.8.9")
                    .And(new Filter(FilterType.Categories, FilterOps.Is, "forge"));

                FilterCollection finalSearch = searchQuery.Or(searchQueryPt2);

                Log.Print(finalSearch.Construct());

                SearchResponse response = await ModrinthV3.Search("", finalSearch, SearchIndex.Relevance, 0, 1);
                FullProject? project = await ModrinthV3.GetProjectAsync(response.Hits[0].Id);
                if (project == null) { break; }

                await ModrinthV3.ResolveInstanceAndProjectToFile(Instance.DefaultFabric(), response.Hits[0].Id);

                break;
            
            case "checky-neoforge":
                await Init();
                await VersionManifest.LoadAsync();
                ClientManifest manifest = await NeoForge.GetClientManifestForGameVersionAsync(args[1]);
                Log.Print(PrettyToString.FromList(manifest.Arguments.Jvm.ToList()));
                Log.Print(PrettyToString.FromList(manifest.Arguments.Game.ToList()));
                await HandleLaunch([], true, manifest);
                break;
            
            case "checky-nfinstall":
                await Init();
                await VersionManifest.LoadAsync();
                string neoForgeVersion = await NeoForge.GetLatestNeoForgeVersionAsync(args[1]);
                Log.Print($"latest neoforge is {neoForgeVersion}");
                ClientManifest newManifest = await NeoForge.GetClientManifestAsync(neoForgeVersion);
                await LaunchMinecraft.DownloadEverything(newManifest);
                await NeoForge.InstallLoader(newManifest, neoForgeVersion);
                await HandleLaunch([], true, newManifest);
                break;

            default:
                Log.PrintErr($"Unknown command: {args[0]}");
                PrintUsage();
                Environment.Exit(1);
                break;
        }
        
    }

    static void PrintUsage()
    {
        Log.Print("hyperlaunch - fastest minecraft launcher around");
        Log.Print(" ");
        Log.Print("Usage:");
        Log.Print("  hyperlaunch-cli login                           Log in with your Microsoft account");
        Log.Print("  hyperlaunch-cli launch [version]                Download & launch a version (default: latest release)");
        Log.Print("  hyperlaunch-cli versions                        List available versions");
        Log.Print("  hyperlaunch-cli launch-fabric                   Download & launch a Fabric modded version");
        Log.Print("  hyperlaunch-cli gen-cmd [version] [fabric]      Generate a launch command");
        Log.Print("  hyperlaunch-cli search-mods [query]             Look for mods at Modrinth");
        Log.Print("  hyperlaunch-cli checky                          >:3");
    }

    static async Task Init()
    {
        string appData  = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Directory.CreateDirectory(Path.Combine(appData,  ".hyperlaunch/"));
        Directory.CreateDirectory(Path.Combine(localApp, ".hyperlaunch/"));
        Directory.CreateDirectory(Path.Combine(localApp, ".hyperlaunch/", "instances/"));

        await Settings.SettingsContainer.LoadAsync(
            Path.Combine(appData, ".hyperlaunch/settings.json"));
    }

    static async Task HandleLogin()
    {
        string msToken = await MSAuth.Login();
        GameAccount account = await GameAccount.CreateAsync(msToken);
        Log.Print($"Logged in as {account.MinecraftUsername}");
    }

    static async Task HandleListVersions()
    {
        await Init();
        await VersionManifest.LoadAsync();
        Log.Print($"Latest release:  {VersionManifest.Data.Latest.Release}");
        Log.Print($"Latest snapshot: {VersionManifest.Data.Latest.Snapshot}");
        Log.Print(" ");
        Log.Print("Recent versions:");
        int count = Math.Min(25, VersionManifest.Data.Versions.Count);
        for (int i = 0; i < count; i++)
        {
            GameVersion v = VersionManifest.Data.Versions[i];
            Log.Print($"  {v.Id,-20} [{v.Type}]");
        }
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
                if (!instance.MainVersion.StartsWith("26")) { Log.Print("Obfuscated NeoForge is not ready yet."); }
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

    static async Task HandleLaunch(string[] args, bool forceModernJava, ClientManifest? overrideClientManifest = null)
    {
        Log.Print("starting launch");
        await Init();
        Log.Print("here");

        if (overrideClientManifest is not null)
        {
            await LaunchMinecraft.AuthAndLaunch(overrideClientManifest, Instance.Default());
        }
        else
        {
            string versionId = args[1];
            await LaunchMinecraft.AuthAndLaunch(versionId, Instance.Default());
        }
    }

    static async Task HandleGenCmd(string[] args)
    {
        await Init();

        string versionId = args[1];
        bool modernJvm = false;
        if (args.Length >= 3)
        {
            bool.TryParse(args[2], out modernJvm);
        }
        ClientManifest manifest = await LaunchMinecraft.SetupClient(versionId, modernJvm);
        string cmd = await LaunchMinecraft.SetupClientAndResolveCmd(versionId, Instance.Default(), modernJvm);
        Log.Print($"Hyperlaunch Launch Command: {cmd}");
    }
}

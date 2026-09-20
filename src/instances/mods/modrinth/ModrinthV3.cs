using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Hyperlaunch.Download;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Instances.Mods.Modrinth;

public static class ModrinthV3
{
    // TODO: this
    #nullable enable

    const string BaseModrinthV3Url = "https://api.modrinth.com/v3/";
    
    public static async Task<SearchResponse> Search(string? query = null, FilterCollection? filters = null, SearchIndex? index = null,
                                                    int? offset = null, int? limit = null)
    {
        Dictionary<string, string> urlParams = new Dictionary<string, string>();
        if (query != null)
        {
            urlParams.Add("query", query);
        }
        if (filters != null)
        {
            urlParams.Add("new_filters", filters.Construct());
        }
        if (index != null)
        {
            urlParams.Add("index", Utilities.StringEnum.Retrieve(index)!);
        }
        if (offset != null)
        {
            urlParams.Add("offset", offset.ToString()!); // is the compiler bad or am I bad. why do i need an ! here.
        }
        if (limit != null)
        {
            urlParams.Add("limit", limit.ToString()!);
        }
        string serialisedParams = await new FormUrlEncodedContent(urlParams).ReadAsStringAsync();
        Log.Print($"requesting a {BaseModrinthV3Url}search?{serialisedParams}");
        HttpResponseMessage res = await Http.Client.GetAsync($"{BaseModrinthV3Url}search?{serialisedParams}");
        res.EnsureSuccessStatusCode();
        SearchResponse? searchResponse = await res.Content.ReadFromJsonAsync(HyperlaunchJsonContext.Default.SearchResponse);
        if (searchResponse == null) { throw new Exception("wahhhhhhh"); }
        
        return searchResponse;
    }

    // i'd like to thank the modrinth api for using the v2 type for some reason
    public static async Task<FullProject?> GetProjectAsync(string projectIdOrSlug)
    {
        Log.Print($"requesting a {BaseModrinthV3Url}project/{projectIdOrSlug}");
        HttpResponseMessage res = await Http.Client.GetAsync($"{BaseModrinthV3Url}project/{projectIdOrSlug}");
        res.EnsureSuccessStatusCode();
        FullProject? project = await res.Content.ReadFromJsonAsync(HyperlaunchJsonContext.Default.FullProject);
        return project;
    }

    public static async Task<ModrinthVersion?> GetVersionAsync(string versionId)
    {
        HttpResponseMessage res = await Http.Client.GetAsync($"{BaseModrinthV3Url}version/{versionId}");
        res.EnsureSuccessStatusCode();
        ModrinthVersion? version = await res.Content.ReadFromJsonAsync(HyperlaunchJsonContext.Default.ModrinthVersion);
        return version;
    }

    public static async Task<ModrinthVersion> ResolveInstanceAndProjectToVersion(Instance instance, string projectId)
    {
        Dictionary<string, string> urlParams = new Dictionary<string, string>
        {
            { "loaders", $"[\"{StringEnum.Retrieve(instance.ClientType)}\"]" },
            { "loader_fields", $"{{\"game_versions\":[\"{instance.MainVersion}\"]}}" },
            { "featured", "false" },
            { "include_changelog", "false" }
        };
        string serialisedParams = await new FormUrlEncodedContent(urlParams).ReadAsStringAsync();

        Log.Print($"requesting a {BaseModrinthV3Url}project/{projectId}/version?{serialisedParams}");
        HttpResponseMessage res = await Http.Client.GetAsync($"{BaseModrinthV3Url}project/{projectId}/version?{serialisedParams}");
        res.EnsureSuccessStatusCode();
        List<ModrinthVersion>? versions = await res.Content.ReadFromJsonAsync(HyperlaunchJsonContext.Default.ListModrinthVersion);
        if (versions == null) { throw new Exception("screaming and crying rn"); }

        return versions[0];
    }

    public static async Task<VersionFile> ResolveInstanceAndProjectToFile(Instance instance, string projectId)
    {
        return (await ResolveInstanceAndProjectToVersion(instance, projectId)).VersionFiles[0];
    }

    public static async Task DownloadMod(Instance instance, string projectId, bool andDependenciesToo = true, string[]? paths = null)
    {
        if (paths == null) { paths = DownloadTask.ScanPaths(); }
        ModrinthVersion version = await ResolveInstanceAndProjectToVersion(instance, projectId);
        Log.Print($"resolved project to version {version.Id}");
        List<Dependency> requiredDeps = version.Dependencies.Where(dependency => dependency.DependencyType == "required").ToList();
        if (requiredDeps.Count > 0)
        {
            foreach (Dependency dependency in requiredDeps)
            {
                await DownloadMod(instance, dependency.ProjectId, paths: paths);
            }
        }
        VersionFile file = version.VersionFiles[0];
        await Cache.SmartGet(file.Url, true, Path.Combine(instance.GetInstancePath(), "mods/", file.Filename), paths);
    }
}

public enum SearchIndex
{
    [Utilities.StringEnum("relevance")]
    Relevance,

    [Utilities.StringEnum("downloads")]
    Downloads,
    
    [Utilities.StringEnum("follows")]
    Follows,
    
    [Utilities.StringEnum("newest")]
    Newest,
    
    [Utilities.StringEnum("updated")]
    Updated
}

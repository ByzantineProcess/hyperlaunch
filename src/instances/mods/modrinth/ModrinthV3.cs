using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hyperlaunch.Instances.Mods.Modrinth;

public static class ModrinthV3
{
    // TODO: this
    #nullable enable

    const string BaseModrinthV3Url = "https://api.modrinth.com/v3/";
    
    public static async Task Search(string? query = null, FilterCollection? filters = null, SearchIndex? index = null,
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
        Log.Print(searchResponse.Hits[0].ToString());
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

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Hyperlaunch.Instances.Mods.Modrinth;


public static class ModrinthV2
{
    #nullable enable

    const string BaseModrinthV2Url = "https://api.modrinth.com/v2/";
    
    public static async Task Search(string? query = null, FacetCollection? facets = null, SearchIndex? index = null,
                                    int? offset = null, int? limit = null)
    {
        Dictionary<string, string> urlParams = new Dictionary<string, string>();
        if (query != null)
        {
            urlParams.Add("query", query);
        }
        if (facets != null)
        {
            urlParams.Add("facets", facets.Serialise());
        }
        if (index != null)
        {
            urlParams.Add("index", Utilities.StringEnum.Retrieve(index));
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
        Log.Print($"requesting a {BaseModrinthV2Url}search?{serialisedParams}");
        HttpResponseMessage res = await Http.Client.GetAsync($"{BaseModrinthV2Url}search?{serialisedParams}");
        res.EnsureSuccessStatusCode();
        Log.Print(await res.Content.ReadAsStringAsync());
    }
}
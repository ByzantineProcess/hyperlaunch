using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hyperlaunch;

public static class Http
{
    public static readonly HttpClient Client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            { "Accept", "application/json" },
            // { "User-Agent", "Hyperlaunch/0.1 (https://github.com/byzantineprocess, discord: byzantineprocess)" }
        }
    };

    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T payload)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null   // C# STOP CAMELCASING MY PRISTINELY PREPARED JSON
        };

        var json = JsonSerializer.Serialize(payload, options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await Client.PostAsync(url, content);
    }
}
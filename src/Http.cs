using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Hyperlaunch;

public static class Http
{
    public static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 8,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AutomaticDecompression = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true,
        };

        var client = new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            Timeout = TimeSpan.FromSeconds(60),
            DefaultRequestHeaders =
            {
                { "Accept", "application/json" },
                { "User-Agent", "Hyperlaunch/0.1 (https://github.com/byzantineprocess, discord: byzantineprocess)" }
            }
        };

        return client;
    }

    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T payload, JsonTypeInfo<T> jsonTypeInfo)
    {
        var memoryStream = new MemoryStream();
        JsonSerializer.Serialize(memoryStream, payload, jsonTypeInfo);
        memoryStream.Position = 0;

        var content = new StreamContent(memoryStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return await Client.PostAsync(url, content);
    }
}
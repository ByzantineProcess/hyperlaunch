using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Hyperlaunch;

public static class Http
{
    public static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        SocketsHttpHandler handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 6,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AutomaticDecompression = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true,
        };

        HttpClient client = new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher, // recommended (?)
            Timeout = TimeSpan.FromSeconds(60),
            DefaultRequestHeaders =
            {
                { "Accept", "application/json" },
                { "User-Agent", "Hyperlaunch/0.1 (made by https://github.com/byzantineprocess, discord: byzantineprocess)" }
            }
        };

        return client;
    }

    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T payload, JsonTypeInfo<T> jsonTypeInfo)
    {
        MemoryStream memoryStream = new MemoryStream();
        JsonSerializer.Serialize(memoryStream, payload, jsonTypeInfo);
        memoryStream.Position = 0;

        StreamContent content = new StreamContent(memoryStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return await Client.PostAsync(url, content);
    }
}

public enum HttpHint // this entire system should not save much time at all but what the hell
{
    OneOne,
    Two,
    Three
}
public static class HttpHintApplier
{
    public static HttpRequestMessage Apply(HttpRequestMessage requestMessage, HttpHint httpHint)
    {
        switch (httpHint)
        {
            case HttpHint.OneOne:
                requestMessage.Version = HttpVersion.Version11;
                break;
            case HttpHint.Two:
                requestMessage.Version = HttpVersion.Version20;
                break;
            case HttpHint.Three:
                requestMessage.Version = HttpVersion.Version30;
                break;
        }
        return requestMessage;
    }
}
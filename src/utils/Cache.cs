using System;
using System.IO;
using System.Text;
using System.IO.Hashing;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.IO.Compression;
using System.Threading;

namespace Hyperlaunch.Utilities;

public static class Cache
{
    public static readonly string BaseCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/cache/");

    #nullable enable
    public static async Task<byte[]?> SmartGet(string url, bool isAlreadyVersioned = false, string? destination = null, string[]? paths = null, HttpHint httpHint = HttpHint.OneOne, SemaphoreSlim? semaphoreSlim = null)
    {
        // this is the start of the Incredibly Complicated Cache System™
        // the name is wrong
        // it's super simple
        // you'll be fine

        // TODO: at some point Cache-Control parsing should be added but i really can't rn

        // the very first step is figuring out what the filename is supposed to be
        // that looks like .hyperlaunch/cache/<domain>/<filename but hashed>

        string filePath = CalculateCachePath(url, paths);
        string rootPath = filePath;
        if (FileExists(filePath + ".dest", paths))
        {
            string destPointer = await File.ReadAllTextAsync(filePath + ".dest");
            filePath = destPointer;
            
        }

        if (FileExists(filePath, paths))
        {
            // a cached copy exists. nice! we still might need to ask remote if it's the latest (unless told otherwise)
            // RULE 1: Global 1h cache since last modification time.
            DateTime lastChanged = File.GetLastWriteTime(filePath);
            DateTime currentTime = DateTime.Now;
            if (lastChanged.AddHours(1) > currentTime || isAlreadyVersioned)
            {
                if (destination != null)
                {
                    return null;
                }
                return await ReadCompressedAsync(filePath);
            }
            
            // RULE 2: Check for any cache info around the file.
            string etagFile = rootPath + ".etag"; // ETag
            string lmtFile = rootPath + ".lmt"; // Last-Modified time
            if (FileExists(etagFile, paths))
            {
                string etag = await File.ReadAllTextAsync(etagFile);
                HttpRequestMessage etaggedRequest = new HttpRequestMessage(HttpMethod.Get, url);
                etaggedRequest.Headers.TryAddWithoutValidation("If-None-Match", etag);
                etaggedRequest = HttpHintApplier.Apply(etaggedRequest, httpHint);
                HttpResponseMessage res = await Http.Client.SendAsync(etaggedRequest);
                ReleaseSemaphore(semaphoreSlim);
                try
                {
                    res.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    if (destination != null)
                    {
                        return null;
                    }
                    return await ReadCompressedAsync(filePath);
                }
                
                if (res.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    if (destination != null)
                    {
                        return null;
                    }
                    return await ReadCompressedAsync(filePath);
                }
                // it's probably a 200 OK?
                
                await CacheHttpResponseAsync(rootPath, res, destination);
                return await res.Content.ReadAsByteArrayAsync();
            }
            if (FileExists(lmtFile, paths))
            {
                string lmt = await File.ReadAllTextAsync(lmtFile);
                HttpRequestMessage lmtRequest = new HttpRequestMessage(HttpMethod.Get, url);
                lmtRequest.Headers.Add("If-Modified-Since", lmt);
                lmtRequest = HttpHintApplier.Apply(lmtRequest, httpHint);
                HttpResponseMessage res = await Http.Client.SendAsync(lmtRequest);
                ReleaseSemaphore(semaphoreSlim);
                try
                {
                    res.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    if (destination != null)
                    {
                        return null;
                    }
                    return await ReadCompressedAsync(filePath);
                }
                if (res.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    if (destination != null)
                    {
                        return null;
                    }
                    return await ReadCompressedAsync(filePath);
                }
                await CacheHttpResponseAsync(rootPath, res, destination);
                return await res.Content.ReadAsByteArrayAsync();
            }
        }
        
        
        HttpRequestMessage normalRequest = new HttpRequestMessage(HttpMethod.Get, url);
        normalRequest = HttpHintApplier.Apply(normalRequest, httpHint);
        HttpResponseMessage resp = await Http.Client.SendAsync(normalRequest);
        ReleaseSemaphore(semaphoreSlim);
        await CacheHttpResponseAsync(rootPath, resp, destination);
        return await resp.Content.ReadAsByteArrayAsync();
    }

    public static async Task<string> SmartGetString(string url, bool isAlreadyVersioned = false, HttpHint httpHint = HttpHint.OneOne)
    {
        // without destination set, SmartGet will never return null.
        #pragma warning disable CS8604
        return Encoding.UTF8.GetString(await SmartGet(url, isAlreadyVersioned, httpHint: httpHint));
        #pragma warning restore CS8604
    }
    public static string CalculateCachePath(string url, string[]? paths)
    {
        Uri parsedUrl = new Uri(url);
        string domain = parsedUrl.DnsSafeHost;
        string ident = BitConverter.ToString(XxHash128.Hash(Encoding.UTF8.GetBytes(parsedUrl.PathAndQuery))).Replace("-", string.Empty).ToLower();
        string res =  Path.Combine(BaseCachePath, domain + "/", ident);
        if (paths == null) return res;
        if (!paths.Contains(Path.GetDirectoryName(res)))
        {
            Directory.CreateDirectory(Path.Combine(BaseCachePath, domain));
        }
        return res;
    }

    public static bool FileExists(string filePath, string[]? paths)
    {
        if (paths == null)
        {
            return File.Exists(filePath);
        }
        else
        {
            return paths.Contains(filePath);
        }
    }

    public static void ReleaseSemaphore(SemaphoreSlim? semaphore)
    {
        if (semaphore != null)
        {
            semaphore.Release();
        }
    }

    public static async Task WriteCompressedAsync(string filePath, byte[] bytes, int recursion = 0)
    {

        try
        {
            await using var fileStream = File.Create(filePath);
            await using var brotliStream = new BrotliStream(fileStream, CompressionMode.Compress);

            await using var memStream = new MemoryStream(bytes);
            await memStream.CopyToAsync(brotliStream);
        }
        catch (IOException)
        {
            if (recursion == 5)
            {
                // yeah it's not working. NOW you can error
                throw;
            }
            // wait a second or two
            await Task.Delay(150*(recursion+1));
            #pragma warning disable CS8604 // if there is a filePath where we're writing to root atp you've got bigger issues
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)); // just in case
            #pragma warning restore CS8604
            await WriteCompressedAsync(filePath, bytes, recursion + 1);
        }
    }

    public static async Task<byte[]> ReadCompressedAsync(string filePath, int recursion = 0)
    {
        try
        {
            byte[] res;
            using (FileStream fileStream = File.OpenRead(filePath))
            using (MemoryStream memStream = new MemoryStream())
            using (BrotliStream brotliStream = new BrotliStream(fileStream, CompressionMode.Decompress))
            {
                await brotliStream.CopyToAsync(memStream);
                memStream.Seek(0, SeekOrigin.Begin);
                res = memStream.ToArray();
            }
            return res;
        }
        catch (IOException)
        {
            if (recursion == 5)
            {
                // yeah it's not working. NOW you can error
                throw;
            }
            // wait a second or two
            await Task.Delay(150*(recursion+1));
            return await ReadCompressedAsync(filePath, recursion + 1);
        }
    }

    public static async Task CacheHttpResponseAsync(string filePath, HttpResponseMessage message, string? destination)
    {
        if (destination == null)
        {
            await WriteCompressedAsync(filePath, await message.Content.ReadAsByteArrayAsync());
        }
        else
        {
            await File.WriteAllBytesAsync(destination, await message.Content.ReadAsByteArrayAsync());
            await File.WriteAllTextAsync(filePath + ".dest", destination);
        }
        if (message.Headers.Contains("ETag"))
        {
            await File.WriteAllTextAsync(filePath + ".etag", message.Headers.GetValues("ETag").First());
            return;
        }
        if (message.Content.Headers.Contains("Last-Modified"))
        {
            await File.WriteAllTextAsync(filePath + ".lmt", message.Content.Headers.GetValues("Last-Modified").First());
        }
    }
}
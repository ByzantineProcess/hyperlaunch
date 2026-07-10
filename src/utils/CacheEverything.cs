using System;
using System.IO;
using System.Text;
using System.IO.Hashing;
using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.IO.Compression;

namespace Hyperlaunch.Utilities;

public static class CacheEverything
{
    public static readonly string BaseCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/cache/");

    public static async Task<byte[]> SmartGet(string url, bool isAlreadyVersioned = false, bool largeFile = false)
    {
        // this is the start of the Incredibly Complicated Cache System™
        // the name is wrong
        // it's super simple
        // you'll be fine

        // TODO: at some point Cache-Control parsing should be added but i really can't rn

        // the very first step is figuring out what the filename is supposed to be
        // that looks like .hyperlaunch/cache/<domain>/<filename but hashed>
        string fileName = CalculateCachePath(url, true);
        if (File.Exists(fileName))
        {
            // a cached copy exists. nice! we still might need to ask remote if it's the latest (unless told otherwise)
            // RULE 1: Global 1h cache since last modification time.
            SafeFileHandle handle = File.OpenHandle(fileName); // TODO: this says it uses Win32 apis, is this Linux safe?
            DateTime lastChanged = File.GetLastWriteTime(handle);
            handle.Close();
            DateTime currentTime = DateTime.Now;
            if (lastChanged.AddHours(1) > currentTime || isAlreadyVersioned)
            {
                return await ReadCompressedAsync(fileName);
            }
            
            // RULE 2: Check for any cache info around the file.
            string etagFile = fileName + ".etag"; // ETag
            string lmtFile = fileName + ".lmt"; // Last-Modified time
            if (File.Exists(etagFile))
            {
                string etag = await File.ReadAllTextAsync(etagFile);
                HttpRequestMessage etaggedRequest = new HttpRequestMessage(HttpMethod.Get, url);
                etaggedRequest.Headers.TryAddWithoutValidation("If-None-Match", etag);
                HttpResponseMessage res = await Http.Client.SendAsync(etaggedRequest);
                try
                {
                    res.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    return await ReadCompressedAsync(fileName);
                }
                
                if (res.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    return await ReadCompressedAsync(fileName);
                }
                // it's probably a 200 OK?
                await CacheHttpResponseAsync(fileName, res);
                return await res.Content.ReadAsByteArrayAsync();
            }
            if (File.Exists(lmtFile))
            {
                string lmt = await File.ReadAllTextAsync(lmtFile);
                HttpRequestMessage lmtRequest = new HttpRequestMessage(HttpMethod.Get, url);
                lmtRequest.Headers.Add("If-Modified-Since", lmt);
                HttpResponseMessage res = await Http.Client.SendAsync(lmtRequest);
                try
                {
                    res.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    return await ReadCompressedAsync(fileName);
                }
                if (res.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    return await ReadCompressedAsync(fileName);
                }
                await CacheHttpResponseAsync(fileName, res);
                return await res.Content.ReadAsByteArrayAsync();
            }
        }
        HttpResponseMessage resp = await Http.Client.GetAsync(url);
        await CacheHttpResponseAsync(fileName, resp);
        return await resp.Content.ReadAsByteArrayAsync();
    }

    public static async Task<string> SmartGetString(string url, bool isAlreadyVersioned = false)
    {
        return Encoding.UTF8.GetString(await SmartGet(url, isAlreadyVersioned));
    }

    public static async Task GetAndCopyFromCache(string url, string destPath, bool isAlreadyVersioned = false)
    {
        string filePath = CalculateCachePath(url, true);
        if (isAlreadyVersioned && File.Exists(filePath))
        {
            File.Copy(filePath, destPath, true);
        }
        byte[] resp = await SmartGet(url);
    }

    public static string CalculateCachePath(string url, bool andCreateDir = false)
    {
        Uri parsedUrl = new Uri(url);
        string domain = parsedUrl.DnsSafeHost;
        if (andCreateDir)
        {
            Directory.CreateDirectory(Path.Combine(BaseCachePath, domain));
        }
        string ident = BitConverter.ToString(XxHash128.Hash(Encoding.UTF8.GetBytes(parsedUrl.PathAndQuery))).Replace("-", string.Empty).ToLower();
        return Path.Combine(BaseCachePath, domain + "/", ident);
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

    public static async Task CacheHttpResponseAsync(string filePath, HttpResponseMessage message)
    {
        await WriteCompressedAsync(filePath, await message.Content.ReadAsByteArrayAsync());
        if (message.Content.Headers.Contains("Last-Modified"))
        {
            await File.WriteAllTextAsync(filePath + ".lmt", message.Content.Headers.GetValues("Last-Modified").First());
        }        
        if (message.Headers.Contains("ETag"))
        {
            await File.WriteAllTextAsync(filePath + ".etag", message.Headers.GetValues("ETag").First());
        }
    }
}
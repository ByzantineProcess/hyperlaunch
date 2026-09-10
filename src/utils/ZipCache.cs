
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Hyperlaunch.Utilities;

// Extension to the Cache system, supporting retrieving files from within remote ZIPs. Mostly intended for use in Forge-like loader handlers.
public static class ZipCache
{
    public static async Task<byte[]> ReadFileFromZipUrl(string url, string path, bool cacheFile)
    {
        string cpath = Cache.CalculateCachePath($"{url}:/{path}", default, false);
        if (File.Exists(Path.GetFullPath(cpath)))
        {
            return await Cache.ReadCompressedAsync(Path.GetFullPath(cpath));
        }
        byte[] zipBytes = await Cache.SmartGet(url, true);
        Stream zipStream = new MemoryStream(zipBytes);
        
        ZipArchive zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        ZipArchiveEntry entry = zipArchive.GetEntry(path);
        if (entry.Length > 1e+8) // hardcoded 100MB limit so zip bombs are never even a possibility
        {
            throw new Exception("this zip is kinda sus ngl");
        }
        if (cacheFile)
        {
            using Stream zipFstreamForCache = entry.Open();
            cpath = Cache.CalculateCachePath($"{url}:/{path}", default, true);
            await Cache.WriteCompressedAsync(cpath, zipFstreamForCache);
        }
        using Stream zipFstream = entry.Open();
        byte[] file = new byte[entry.Length];
        zipFstream.ReadExactly(file);
        return file;
    }
}
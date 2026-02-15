
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hyperlaunch;

namespace Hyperlaunch.Download;

public class DownloadTask
{
    public static readonly string BaseAssetUrl = "https://resources.download.minecraft.net/";
    public static readonly string BasePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/");
    public static readonly string BaseAssetPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "assets/");
    public static readonly string BaseLibraryPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "libraries/");
    public static readonly string BaseVersionPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "versions/");
    public string Url { get; set; }
    public string DestinationPath { get; set; }
    public string ExpectedHash { get; set; }
    public long ExpectedSize { get; set; }
    public DownloadTask(string url, string destinationPath, string expectedHash, long expectedSize = 0)
    {
        Url = url;
        DestinationPath = destinationPath;
        ExpectedHash = expectedHash;
        ExpectedSize = expectedSize;
    }
    public static List<DownloadTask> FromClientJson(ClientManifest clientManifest)
    {
        var tasks = new List<DownloadTask>();

        foreach (var asset in clientManifest.AssetIndex.Index.Objects.Values.ToList())
        {
            // url is base + first 2 chars of hash + rest of hash
            var path = asset.Sha1[..2] + "/" + asset.Sha1;
            var url = BaseAssetUrl + path;
            tasks.Add(new DownloadTask(url, $"{BaseAssetPath}/{path}", asset.Sha1, asset.Size));
        }

        foreach (var library in clientManifest.Libraries)
        {
            if (library.Downloads != null && library.Downloads.Artifact != null)
            {
                var artifact = library.Downloads.Artifact;
                tasks.Add(new DownloadTask(artifact.Url, $"{BaseLibraryPath}/{library.Name.Replace(':', '-')}/{artifact.Path}", artifact.Sha1, artifact.Size));
            }
        }

        var mainJar = clientManifest.Downloads.Client;
        tasks.Add(new DownloadTask(mainJar.Url, $"{BaseVersionPath}/{clientManifest.Id}.jar", mainJar.Sha1, mainJar.Size));

        return tasks;
    }
    public async Task<long> ExecuteAsync(SemaphoreSlim networkLimiter = null)
    {
        byte[] data;
        if (networkLimiter != null)
        {
            await networkLimiter.WaitAsync();
            try
            {
                // network concurrency is limited; hashing/IO remain unbounded
                data = await Http.Client.GetByteArrayAsync(Url);
            }
            finally
            {
                networkLimiter.Release();
            }
        }
        else
        {
            data = await Http.Client.GetByteArrayAsync(Url);
        }

        if (!Sha1.Verify(data, ExpectedHash))
        {
            throw new System.Exception("Downloaded file failed integrity check.");
        }
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DestinationPath));
        System.IO.File.WriteAllBytes(DestinationPath, data);
        return data.LongLength;
    }
    public static void EnsureAllDirectoriesExist()
    {
        System.IO.Directory.CreateDirectory(BaseAssetPath);
        System.IO.Directory.CreateDirectory(BaseLibraryPath);
        System.IO.Directory.CreateDirectory(BaseVersionPath);
    }
    public static async Task ExecuteAllAsync(List<DownloadTask> tasks, int maxConcurrentNetwork = 4, System.IProgress<long> bytesRemainingProgress = null)
    {
        EnsureAllDirectoriesExist();

        long totalBytesRemaining = tasks.Sum(task => task.ExpectedSize > 0 ? task.ExpectedSize : 0);
        if (totalBytesRemaining > 0)
        {
            bytesRemainingProgress?.Report(totalBytesRemaining);
        }

        async Task RunTaskAsync(DownloadTask task, SemaphoreSlim limiter)
        {
            var downloaded = await task.ExecuteAsync(limiter);
            if (bytesRemainingProgress == null)
            {
                return;
            }

            var delta = task.ExpectedSize > 0 ? task.ExpectedSize : downloaded;
            if (delta <= 0)
            {
                return;
            }

            var remaining = Interlocked.Add(ref totalBytesRemaining, -delta);
            bytesRemainingProgress.Report(remaining < 0 ? 0 : remaining);
        }

        if (maxConcurrentNetwork <= 0)
        {
            var unlimitedTasks = tasks.Select(task => RunTaskAsync(task, null)).ToList();
            await Task.WhenAll(unlimitedTasks);
            return;
        }

        using var networkLimiter = new SemaphoreSlim(maxConcurrentNetwork, maxConcurrentNetwork);
        var limitedTasks = tasks.Select(task => RunTaskAsync(task, networkLimiter)).ToList();
        await Task.WhenAll(limitedTasks);
    }

}
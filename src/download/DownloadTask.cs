
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hyperlaunch;

namespace Hyperlaunch.Download;

public class DownloadTask
{
    public static readonly string BaseAssetUrl = "https://resources.download.minecraft.net/";
    public static readonly string BasePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/");
    public static readonly string BaseAssetPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "assets/objects/");
    public static readonly string BaseLibraryPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "libraries/");
    public static readonly string BaseVersionPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "versions/");
    public string Url { get; set; }
    public string DestinationPath { get; set; }
    public List<string> ExpectedHashes { get; set; }
    public long ExpectedSize { get; set; }
    public DownloadTask(string url, string destinationPath, string expectedHash, long expectedSize = 0)
    {
        Url = url;
        DestinationPath = destinationPath;
        ExpectedHashes = expectedHash != null ? new List<string> { expectedHash } : new List<string>();
        ExpectedSize = expectedSize;
    }
    public DownloadTask(string url, string destinationPath, List<string> expectedHashes, long expectedSize = 0)
    {
        Url = url;
        DestinationPath = destinationPath;
        ExpectedHashes = expectedHashes ?? new List<string>();
        ExpectedSize = expectedSize;
    }
    public static List<DownloadTask> FromClientJson(ClientManifest clientManifest)
    {
        var tasks = new List<DownloadTask>();

        foreach (var asset in clientManifest.AssetIndex.Index.Objects.Values.ToList())
        {
            var path = asset.Sha1[..2] + "/" + asset.Sha1;
            var destinationPath = $"{BaseAssetPath}/{path}";
            if (System.IO.File.Exists(destinationPath))
            {
                continue;
            }

            var url = BaseAssetUrl + path;
            tasks.Add(new DownloadTask(url, destinationPath, asset.Sha1, asset.Size));
        }

        if (clientManifest.Logging?.Client != null)
        {
            var loggingUrl = clientManifest.Logging.Client.File.Url;
            var loggingDestination = $"{BasePath}/configs/logging/{clientManifest.Logging.Client.File.Id}";
            tasks.Add(new DownloadTask(loggingUrl, loggingDestination, clientManifest.Logging.Client.File.Sha1, clientManifest.Logging.Client.File.Size));
        }

        var os = OsInfo.Detect();
        foreach (var library in clientManifest.ResolveLibraries(os))
        {
            // Download native classifier artifact for current platform
            var nativeArtifact = library.GetNativeArtifact(os);
            if (nativeArtifact != null)
            {
                var destinationPath = $"{BaseLibraryPath}/{nativeArtifact.Path}";
                if (!System.IO.File.Exists(destinationPath))
                {
                    tasks.Add(new DownloadTask(nativeArtifact.Url, destinationPath, nativeArtifact.Sha1, nativeArtifact.Size));
                    Log.Print($"Added download task for native artifact of library {library.Name} at URL {nativeArtifact.Url}");
                }
            }

            // Download main artifact
            if (library.Downloads?.Artifact != null)
            {
                var artifact = library.Downloads.Artifact;
                var destinationPath = $"{BaseLibraryPath}/{artifact.Path}";
                if (!System.IO.File.Exists(destinationPath))
                {
                    tasks.Add(new DownloadTask(artifact.Url, destinationPath, artifact.Sha1, artifact.Size));
                }
            }
            else if (library.Name != null && library.Natives == null) // prefer not guessing URLs when natives are present
            {
                // Maven-style library: construct path and URL from name + optional repository url.
                var expectedPath = library.GetExpectedPath();
                var destinationPath = $"{BaseLibraryPath}/{expectedPath}";
                if (!System.IO.File.Exists(destinationPath))
                {
                    var url = library.GetMavenDownloadUrl();
                    var hashes = library.Checksums ?? new List<string>();
                    tasks.Add(new DownloadTask(url, destinationPath, hashes));
                }
            }

            
        }

        var jarVersion = clientManifest.JarVersion ?? clientManifest.Id;
        var mainJar = clientManifest.Downloads.Client;
        var mainJarPath = $"{BaseVersionPath}/{jarVersion}.jar";
        if (!System.IO.File.Exists(mainJarPath))
        {
            tasks.Add(new DownloadTask(mainJar.Url, mainJarPath, mainJar.Sha1, mainJar.Size));
        }

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
            catch (System.Exception ex)
            {
                Log.Print($"Error downloading {Url}: {ex.Message}");
                throw;
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

        if (ExpectedHashes.Count > 0 && !ExpectedHashes.Any(hash => Sha1.Verify(data, hash)))
        {
            // some versions of forge (namely 1.8.9) have incorrect hashes in their manifests. 
            // if the URL is from the main minecraft library repository, it's definitely wrong.
            if (Url.StartsWith("https://libraries.minecraft.net/"))
            {
                throw new System.Exception($"Integrity check failed for {Url}, and since the URL is from the main minecraft library repository, the expected hash in the manifest is likely incorrect. Aborting download to avoid caching invalid file. Please report this to the modpack author.");
            }
            else
            {
                Log.Print($"Integrity check failed for {Url}, but since the URL is not from the main minecraft library repository, the expected hash in the manifest may be incorrect. Caching downloaded file anyway, but it may cause issues later. Please report this to the modpack author.");
            }
        }
        // Directory is expected to be pre-created by ExecuteAllAsync;
        // create on demand only when called standalone.
        var dir = System.IO.Path.GetDirectoryName(DestinationPath);
        if (dir != null && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }
        await System.IO.File.WriteAllBytesAsync(DestinationPath, data);
        return data.LongLength;
    }
    /// <summary>
    /// Extracts native libraries from classifier JARs into the natives directory.
    /// Only extracts native files (.dll, .so, .dylib, .jnilib) while respecting ExtractRules.
    /// </summary>
    public static void ExtractNatives(ClientManifest clientManifest, string nativesDir)
    {
        var os = OsInfo.Detect();
        System.IO.Directory.CreateDirectory(nativesDir);

        foreach (var library in clientManifest.ResolveLibraries(os))
        {
            var nativeArtifact = library.GetNativeArtifact(os);
            if (nativeArtifact == null) continue;

            var jarPath = System.IO.Path.Combine(BaseLibraryPath, nativeArtifact.Path.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(jarPath)) continue;

            using var archive = ZipFile.OpenRead(jarPath);
            foreach (var entry in archive.Entries)
            {
                // Skip directories
                if (string.IsNullOrEmpty(entry.Name)) continue;

                // Check extract exclusion rules
                if (library.Extract?.Exclude != null)
                {
                    bool excluded = false;
                    foreach (var pattern in library.Extract.Exclude)
                    {
                        if (entry.FullName.StartsWith(pattern))
                        {
                            excluded = true;
                            break;
                        }
                    }
                    if (excluded) continue;
                }

                // Only extract native library files
                var ext = System.IO.Path.GetExtension(entry.Name).ToLowerInvariant();
                if (ext == ".dll" || ext == ".so" || ext == ".dylib" || ext == ".jnilib")
                {
                    var destPath = System.IO.Path.Combine(nativesDir, entry.Name);
                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }
        }
    }

    public static void EnsureAllDirectoriesExist()
    {
        System.IO.Directory.CreateDirectory(BaseAssetPath);
        System.IO.Directory.CreateDirectory(BaseLibraryPath);
        System.IO.Directory.CreateDirectory(BaseVersionPath);
    }
    public static async Task ExecuteAllAsync(List<DownloadTask> tasks, int maxConcurrentNetwork = 128, System.IProgress<long> bytesRemainingProgress = null)
    {
        EnsureAllDirectoriesExist();

        // Pre-create all destination directories in one pass to avoid
        // repeated CreateDirectory calls and lock contention during downloads.
        var dirs = new HashSet<string>();
        foreach (var task in tasks)
        {
            var dir = System.IO.Path.GetDirectoryName(task.DestinationPath);
            if (dir != null) dirs.Add(dir);
        }
        foreach (var dir in dirs)
        {
            System.IO.Directory.CreateDirectory(dir);
        }

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
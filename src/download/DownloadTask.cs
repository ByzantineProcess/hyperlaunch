
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Download;

public class DownloadTask
{
    public static readonly string BaseAssetUrl = "https://resources.download.minecraft.net/";
    public static readonly string BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/");
    public static readonly string BaseAssetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "assets/objects/");
    public static readonly string BaseLibraryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "libraries/");
    public static readonly string BaseVersionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "versions/");
    public static readonly string BaseConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "configs/");
    public string Url { get; set; }
    public string DestinationPath { get; set; }
    public List<string> ExpectedHashes { get; set; }
    public long ExpectedSize { get; set; }
    public HttpHint HttpVersionHint{ get; set; }
    public DownloadTask(string url, string destinationPath, string expectedHash, long expectedSize = 0, HttpHint httpHint = HttpHint.Two)
    {
        Url = url;
        DestinationPath = destinationPath;
        ExpectedHashes = expectedHash != null ? new List<string> { expectedHash } : new List<string>();
        ExpectedSize = expectedSize;
        HttpVersionHint = httpHint;
    }
    public DownloadTask(string url, string destinationPath, List<string> expectedHashes, long expectedSize = 0, HttpHint httpHint = HttpHint.Two)
    {
        Url = url;
        DestinationPath = destinationPath;
        ExpectedHashes = expectedHashes ?? new List<string>();
        ExpectedSize = expectedSize;
        HttpVersionHint = httpHint;
    }
    public static DownloadTask[] FromClientJson(ClientManifest clientManifest)
    {
        var tasks = new List<DownloadTask>();
        EnsureAllDirectoriesExist();
        string[] paths = ScanPaths();

        foreach (var asset in clientManifest.AssetIndex.Index.Objects.Values.ToList())
        {
            string path = asset.Sha1[..2] + "/" + asset.Sha1;
            string destinationPath = Path.GetFullPath($"{BaseAssetPath}/{path}");
            
            if (paths.Contains(destinationPath))
            {
                continue;
            }
            

            var url = BaseAssetUrl + path;
            tasks.Add(new DownloadTask(url, destinationPath, asset.Sha1, asset.Size));
        }

        if (clientManifest.Logging?.Client != null)
        {
            var loggingUrl = clientManifest.Logging.Client.File.Url;
            var loggingDestination = Path.GetFullPath($"{BasePath}/configs/logging/{clientManifest.Logging.Client.File.Id}");
            if (!paths.Contains(loggingDestination))
            {
                tasks.Add(new DownloadTask(loggingUrl, loggingDestination, clientManifest.Logging.Client.File.Sha1, clientManifest.Logging.Client.File.Size));
            }
        }

        var os = OsInfo.Detect();
        foreach (var library in clientManifest.ResolveLibraries(os))
        {
            // Download native classifier artifact for current platform
            var nativeArtifact = library.GetNativeArtifact(os);
            if (nativeArtifact != null)
            {
                var destinationPath = Path.GetFullPath($"{BaseLibraryPath}/{nativeArtifact.Path}");
                if (!paths.Contains(destinationPath))
                {
                    tasks.Add(new DownloadTask(nativeArtifact.Url, destinationPath, nativeArtifact.Sha1, nativeArtifact.Size));
                    Log.Print($"Added download task for native artifact of library {library.Name} at URL {nativeArtifact.Url}");
                }
            }

            // Download main artifact
            if (library.Downloads?.Artifact != null)
            {
                var artifact = library.Downloads.Artifact;
                var destinationPath = Path.GetFullPath($"{BaseLibraryPath}/{artifact.Path}");
                if (!paths.Contains(destinationPath))
                {
                    tasks.Add(new DownloadTask(artifact.Url, destinationPath, artifact.Sha1, artifact.Size));
                }
            }
            else if (library.Name != null && library.Natives == null) // prefer not guessing URLs when natives are present
            {
                // Maven-style library: construct path and URL from name + optional repository url.
                var expectedPath = library.GetExpectedPath();
                var destinationPath = Path.GetFullPath($"{BaseLibraryPath}/{expectedPath}");
                if (!paths.Contains(destinationPath))
                {
                    var url = library.GetMavenDownloadUrl();
                    var hashes = library.Checksums ?? new List<string>();
                    tasks.Add(new DownloadTask(url, destinationPath, hashes));
                }
            }
        }

        var jarVersion = clientManifest.JarVersion ?? clientManifest.Id;
        var mainJar = clientManifest.Downloads.Client;
        var mainJarPath = Path.GetFullPath($"{BaseVersionPath}/{jarVersion}.jar");
        if (!paths.Contains(mainJarPath))
        {
            tasks.Add(new DownloadTask(mainJar.Url, mainJarPath, mainJar.Sha1, mainJar.Size));
        }

        DownloadTask[] tasksArray = tasks.ToArray();
        Random.Shared.Shuffle(tasksArray); // better chance of maxing out the bandwidth at any given point

        return tasksArray;
    }
    public async Task<long> ExecuteAsync(string[] paths, SemaphoreSlim networkLimiter = null)
    {
        byte[] data;
        if (networkLimiter != null)
        {
            await networkLimiter.WaitAsync();
            try
            {
                // network concurrency is limited; hashing/IO remain unbounded
                data = await Cache.SmartGet(Url, true, DestinationPath, paths, httpHint: HttpVersionHint);
            }
            catch (Exception ex)
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
            data = await Cache.SmartGet(Url, true, DestinationPath, paths, httpHint: HttpVersionHint);
        }

        if (ExpectedHashes.Count > 0 && !ExpectedHashes.Any(hash => Sha1.Verify(data, hash)))
        {
            Log.Print($"Hash check failed, Url: {Url}, Got hash: {Convert.ToHexString(SHA1.HashData(data)).ToLowerInvariant()}, Expected: {string.Join(", ", ExpectedHashes)}");
            // some versions of forge (namely 1.8.9) have incorrect hashes in their manifests. 
            // if the URL is from the main minecraft library repository, it's definitely wrong.
            if (Url.StartsWith("https://libraries.minecraft.net/") || Url.StartsWith("https://resources.download.minecraft.net/") || Url.StartsWith("https://piston-data.minecraft.net/"))
            {
                throw new System.Exception($"Integrity check failed for {Url}, and since the URL is from the main minecraft repository, the expected hash in the manifest is likely incorrect. Aborting download to avoid caching invalid file. Please report this to the modpack author. {Convert.ToHexString(SHA1.HashData(data)).ToLowerInvariant()}");
            }
            else
            {
                throw new System.Exception($"Integrity check failed for {Url}, but since the URL is not from the main minecraft library repository, the expected hash in the manifest may be incorrect. Caching downloaded file anyway, but it may cause issues later. Please report this to the modpack author. {Convert.ToHexString(SHA1.HashData(data)).ToLowerInvariant()}");
            }
        }
        // Directory is expected to be pre-created by ExecuteAllAsync;
        // create on demand only when called standalone.
        // var dir = Path.GetDirectoryName(DestinationPath);
        // if (dir != null && !Directory.Exists(dir))
        // {
        //     Directory.CreateDirectory(dir);
        // }
        // await File.WriteAllBytesAsync(DestinationPath, data);
        return data.LongLength;
    }
    /// <summary>
    /// Extracts native libraries from classifier JARs into the natives directory.
    /// Only extracts native files (.dll, .so, .dylib, .jnilib) while respecting ExtractRules.
    /// </summary>
    public static void ExtractNatives(ClientManifest clientManifest, string nativesDir, bool forceExtract = false)
    {
        OsInfo os = OsInfo.Detect();
        Directory.CreateDirectory(nativesDir);
        string[] paths = Directory.GetFiles(nativesDir);

        foreach (Library library in clientManifest.ResolveLibraries(os))
        {
            LibraryArtifact nativeArtifact = library.GetNativeArtifact(os);
            if (nativeArtifact == null) continue;

            string jarPath = Path.Combine(BaseLibraryPath, nativeArtifact.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!Cache.FileExists(jarPath, paths)) continue;

            using ZipArchive archive = ZipFile.OpenRead(jarPath);
            foreach (ZipArchiveEntry entry in archive.Entries)
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
                var ext = Path.GetExtension(entry.Name).ToLowerInvariant();
                if (ext == ".dll" || ext == ".so" || ext == ".dylib" || ext == ".jnilib")
                {
                    var destPath = Path.Combine(nativesDir, entry.Name);
                    if (Cache.FileExists(destPath, paths) && !forceExtract)
                    {
                        continue;
                    }
                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }
        }
    }

    public static void EnsureAllDirectoriesExist()
    {
        Directory.CreateDirectory(BaseAssetPath);
        Directory.CreateDirectory(BaseLibraryPath);
        Directory.CreateDirectory(BaseVersionPath);
    }

    public static string[] ScanPaths()
    {
        EnumerationOptions enumOptions = new EnumerationOptions();
        enumOptions.RecurseSubdirectories = true;
        string[] cacheDirFiles = Directory.GetFiles(Cache.BaseCachePath, "*", enumOptions);
        string[] assetDirFiles = Directory.GetFiles(BaseAssetPath, "*", enumOptions);
        string[] libDirFiles = Directory.GetFiles(BaseLibraryPath, "*", enumOptions);
        string[] versionDirFiles = Directory.GetFiles(BaseVersionPath, "*", enumOptions);
        string[] configDirFiles = Directory.GetFiles(BaseConfigPath, "*", enumOptions);
        string[] allDirs = Directory.GetDirectories(BasePath, "*", enumOptions);
        // normalise EVERYTHING
        string[] res = [..cacheDirFiles, ..assetDirFiles, ..libDirFiles, ..versionDirFiles, ..configDirFiles, ..allDirs];
        res = res.Select(x => Path.GetFullPath(x)).ToArray();
        return res;
    }

    public static async Task ExecuteAllAsync(List<DownloadTask> tasks, int maxConcurrentNetwork = 10, IProgress<long> bytesRemainingProgress = null)
    {
        EnsureAllDirectoriesExist();
        string[] paths = ScanPaths();

        var dirs = new List<string>();
        foreach (var task in tasks)
        {
            var dir = Path.GetDirectoryName(task.DestinationPath);
            if ((dir != null) && !paths.Contains(dir)) dirs.Add(dir);
        }
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(dir);
        }

        long totalBytesRemaining = tasks.Sum(task => task.ExpectedSize > 0 ? task.ExpectedSize : 0);
        if (totalBytesRemaining > 0)
        {
            bytesRemainingProgress?.Report(totalBytesRemaining);
        }

        async Task RunTaskAsync(DownloadTask task, SemaphoreSlim limiter, string[] paths)
        {
            var downloaded = await task.ExecuteAsync(paths, limiter);
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
            var unlimitedTasks = tasks.Select(task => RunTaskAsync(task, null, paths)).ToList();
            await Task.WhenAll(unlimitedTasks);
            return;
        }

        using var networkLimiter = new SemaphoreSlim(maxConcurrentNetwork, maxConcurrentNetwork);
        var limitedTasks = tasks.Select(task => RunTaskAsync(task, networkLimiter, paths)).ToList();
        await Task.WhenAll(limitedTasks);
    }

}
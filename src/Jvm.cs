
// different versions of the game require different JVMs:
//   Java 8  for anything using LaunchWrapper (Forge ≤ 1.12.2, old modded versions)
//   Java 17 for 1.17–1.20
//   Java 21 for 1.21+
// Java 21 is NOT backwards-compatible with LaunchWrapper because it casts
// ClassLoader.getSystemClassLoader() directly to URLClassLoader, which
// broke in Java 9 when AppClassLoader stopped extending URLClassLoader.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class Jvm
{
    public string ExecPath { get; private set; } = "java";
    public int Version { get; private set; } = 0; // 0 = unknown

    private Jvm(string execPath, int version)
    {
        ExecPath = execPath;
        Version  = version;
    }

    /// <summary>Creates a Jvm for the given executable, detecting its major version.</summary>
    public Jvm(string execPath)
    {
        ExecPath = execPath;
        Version  = DetectVersion(execPath);
    }

    /// <summary>
    /// Runs <c>java -version</c> and parses the major version number.
    /// Returns 0 if the version cannot be determined.
    /// </summary>
    public static int DetectVersion(string execPath)
    {
        try
        {
            var psi = new ProcessStartInfo(execPath, "-version")
            {
                RedirectStandardError  = true,  // java -version writes to stderr
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            using var proc = Process.Start(psi)!; // non-null: UseShellExecute=false always returns a Process
            // version info comes on stderr for all JVM implementations
            string output = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            // Examples:
            //   java version "1.8.0_401"   → major 8
            //   openjdk version "17.0.10"  → major 17
            //   openjdk version "21.0.2"   → major 21
            var m = Regex.Match(output, @"""(?:1\.)?(\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int major))
                return major;
        }
        catch { /* executable not found or failed to run */ }
        return 0;
    }

    /// <summary>
    /// Scans all common JVM installation directories on the current OS and
    /// returns every JVM found, sorted by major version descending.
    /// </summary>
    public static List<Jvm> ScanForJvms()
    {
        var candidates = new List<string>();

        // ── 1. Whatever is on PATH ──────────────────────────────────────
        string pathExe = OperatingSystem.IsWindows() ? "javaw.exe" : "java";
        candidates.Add(pathExe); // resolved via PATH by Process.Start

        // ── 2. JAVA_HOME ────────────────────────────────────────────────
        string? javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome))
        {
            string bin = Path.Combine(javaHome, "bin", pathExe);
            if (File.Exists(bin)) candidates.Add(bin);
        }

        // ── 3. Common installation roots ────────────────────────────────
        var roots = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            foreach (string pf in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            })
            {
                if (string.IsNullOrEmpty(pf)) continue;
                // Adoptium / Temurin, Microsoft, Oracle, Amazon Corretto, Azul Zulu, Liberica, …
                roots.Add(Path.Combine(pf, "Eclipse Adoptium"));
                roots.Add(Path.Combine(pf, "Microsoft"));
                roots.Add(Path.Combine(pf, "Java"));
                roots.Add(Path.Combine(pf, "Amazon Corretto"));
                roots.Add(Path.Combine(pf, "Azul Systems", "Zulu"));
                roots.Add(Path.Combine(pf, "BellSoft"));
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            roots.Add("/usr/lib/jvm");
            roots.Add("/usr/java");
            roots.Add("/opt/java");
            roots.Add("/opt/jdk");
        }
        else if (OperatingSystem.IsMacOS())
        {
            roots.Add("/Library/Java/JavaVirtualMachines");
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            roots.Add(Path.Combine(home, "Library", "Java", "JavaVirtualMachines"));
        }

        foreach (string root in roots)
        {
            if (!Directory.Exists(root)) continue;
            foreach (string dir in Directory.EnumerateDirectories(root))
            {
                // Search up to two levels deep to cover jre/bin and jdk/bin layouts
                foreach (string subdir in new[] { dir, Path.Combine(dir, "jre") })
                {
                    string bin = Path.Combine(subdir, "bin", pathExe);
                    if (File.Exists(bin)) candidates.Add(bin);
                }
                // macOS layout: Contents/Home/bin/java
                string macBin = Path.Combine(dir, "Contents", "Home", "bin", pathExe);
                if (File.Exists(macBin)) candidates.Add(macBin);
            }
        }

        // Detect versions and deduplicate
        var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<Jvm>();
        foreach (string path in candidates)
        {
            if (!seen.Add(path)) continue;
            int v = DetectVersion(path);
            if (v > 0) results.Add(new Jvm(path, v));
        }

        return results.OrderByDescending(j => j.Version).ToList();
    }

    /// <summary>
    /// Finds the best installed JVM whose major version satisfies
    /// <paramref name="requiredMajor"/>. Prefers the lowest qualifying version
    /// so that e.g. Java 8 is chosen over Java 21 when 8 is needed,
    /// maximising compatibility with legacy LaunchWrapper code.
    /// </summary>
    public static Jvm? FindBestForVersion(int requiredMajor)
    {
        var all = ScanForJvms();
        return all
            .Where(j => j.Version >= requiredMajor)
            .OrderBy(j => j.Version)   // lowest satisfying version first
            .FirstOrDefault();
    }
}
using System;

namespace Hyperlaunch.Download;

public class OsInfo
{
    public string Name { get; set; }
    public System.Version Version { get; set; }
    public string Arch { get; set; }
    public string IntArch => Arch switch
    {
        "x86" => "32",
        "x86_64" => "64",
        "aarch64" => "64",
        _ => "64"
    };

    public static OsInfo Detect()
    {
        string name;
        if (OperatingSystem.IsWindows()) name = "windows";
        else if (OperatingSystem.IsMacOS()) name = "osx";
        else name = "linux";

        return new OsInfo
        {
            Name = name,
            Version = Environment.OSVersion.Version,
            Arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.X86 => "x86",
                System.Runtime.InteropServices.Architecture.X64 => "x86_64",
                System.Runtime.InteropServices.Architecture.Arm64 => "aarch64",
                _ => "unknown"
            }
        };
    }
}
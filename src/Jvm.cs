
// even though different versions of the game recommend different JVMs (8 for <1.20, 17 for 1.20, 21 for 1.21+),
// Java 21 is pretty much fully backwards compatible, so we just use that for everything.

using System.Collections.Generic;

public class Jvm
{
    public string ExecPath { get; private set; } = "java"; // assume it's on the path for now, maybe add a setting for it later
    public int Version { get; private set; } = 21;

    public Jvm(string execPath)
    {
        this.ExecPath = execPath;
        
    }

    public static List<Jvm> ScanForJvms()
    {
        // TODO: do this instead of using the one on path
        return new List<Jvm> { new Jvm("java") };
    }
}
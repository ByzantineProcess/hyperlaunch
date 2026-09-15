

using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

// WIP
public static class ClassFile
{
    public static void ApplyMappingsToJar(string jarPath)
    {
        ZipArchive readmodeJar = ZipFile.OpenRead(jarPath);
        List<string> zipEntries = readmodeJar.Entries.Select(entry => entry.FullName).ToList();
        readmodeJar.Dispose();


        
    }
}
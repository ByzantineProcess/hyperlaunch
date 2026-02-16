
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hyperlaunch.Download;

namespace Hyperlaunch.Launch;

public class LaunchMinecraft
{
    public static void Launch(Jvm jvm, GameAccount account, ClientManifest clientManifest)
    {
        // resolve launch command from client manifest
        List<string> launchCommand = clientManifest.ResolveLaunchCommand(account);
        // if on windows, normalise all / to \ in arguments
        if (OsInfo.Detect().Name == "windows")
        {
            launchCommand = launchCommand.Select(arg => 
                arg.Replace("/", "\\").Replace("\\\\", "\\")).ToList();
        }
        
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = jvm.ExecPath;
        // use ArgumentList to properly handle arguments with spaces
        foreach (var arg in launchCommand)
        {
            startInfo.ArgumentList.Add(arg);
        }
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        var process = System.Diagnostics.Process.Start(startInfo);
        // redirect stdout to a log file
        System.IO.StreamWriter logStream = new System.IO.StreamWriter(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), ".hyperlaunch/", "game.log"));
        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                logStream.WriteLine(args.Data);
                logStream.Flush();
            }
        };
    }
}

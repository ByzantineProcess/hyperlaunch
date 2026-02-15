
using System.Collections.Generic;
using Hyperlaunch.Download;

namespace Hyperlaunch.Launch;

public class LaunchMinecraft
{
    public static void Launch(Jvm jvm, GameAccount account, ClientManifest clientManifest)
    {
        // resolve launch command from client manifest
        List<string> launchCommand = clientManifest.ResolveLaunchCommand(account);
        // use system diagnostics process to launch the game with the resolved command
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = jvm.ExecPath;
        startInfo.Arguments = string.Join(" ", launchCommand);
        System.Diagnostics.Process.Start(startInfo);
    }
}

using System.Diagnostics;

namespace Hyperlaunch.Preheat;

// Java spends some time doing JIT compilation and other warmup tasks when launching.
// Minecraft also spends a lot of time DataFixerUpper-ing and other things on launch.
// What if we could load a JVM with the classpath, and stop it before the Main minecraft class?
public class PreheatedJvm
{
    Jvm baseJvm;
    Process jvmProcess;
    ProcessStartInfo jvmStartInfo;
    public PreheatedJvm(Jvm jvm)
    {
        baseJvm = jvm;
    }

    public void Preheat()
    {
        
    }
}

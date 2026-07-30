using System.Threading.Tasks;

namespace Hyperlaunch.Instances.Mods;

public abstract class ModSource
{
    public abstract bool? CheckForUpdate();
    public abstract Task Download();
    public abstract string GetFilePath();
}
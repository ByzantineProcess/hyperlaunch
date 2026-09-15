using System.Runtime.CompilerServices;
using Hyperlaunch.Utilities;

namespace Hyperlaunch.Instances;

public class ClientJar
{
    public ClientType kind;
}

public enum ClientType
{
    [StringEnum("vanilla")]
    Vanilla,
    [StringEnum("forge")]
    Forge,
    [StringEnum("fabric")]
    Fabric,
    [StringEnum("neoforge")]
    NeoForge,
    [StringEnum("quilt")]
    Quilt,
    [StringEnum("other")]
    Custom
}
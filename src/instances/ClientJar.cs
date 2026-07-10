namespace Hyperlaunch.Instances;

public class ClientJar
{
    public ClientType kind;
}

public enum ClientType
{
    Vanilla,
    Forge,
    Fabric,
    NeoForge,
    Quilt,
    Custom
}
namespace Hyperlaunch.Instances;

public class ClientJar
{
    public Kind kind;
}

public enum Kind
{
    Vanilla,
    ModLoader,
    Custom
}

public enum ModLoader
{
    Forge,
    Fabric,
    NeoForge,
    Babric,
    LegacyFabric,
    LiteLoader,
    RisugamiModLoader,
    NilLoader,
    Quilt,
    Rift
}
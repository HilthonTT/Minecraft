namespace Minecraft.Core.Entities;

/// <summary>
/// Written into spawn packets as its underlying number, so existing entries keep their order and new ones
/// go on the end.
/// </summary>
public enum EntityType
{
    Player,
    Dummy,
    Sheep,
    Zombie,
    Pig,
    Cow
}

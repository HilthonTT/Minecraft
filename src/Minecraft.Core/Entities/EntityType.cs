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
    Cow,

    /// <summary>
    /// A stack lying on the ground waiting to be walked over. Not a mob, and so not built by
    /// <see cref="Mobs.MobFactory"/>, and it carries what it is a stack of, which no other entity does — it
    /// has a spawn packet of its own for that reason.
    /// </summary>
    DroppedItem,
}

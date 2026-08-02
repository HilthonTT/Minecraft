using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>Builds a mob from the entity type a spawn packet carries.</summary>
public static class MobFactory
{
    /// <summary>Null when the type is not a mob, which is what a client does with a type it cannot place.</summary>
    public static Mob? Create(EntityType entityType, int id, World world, Vector3 position) => entityType switch
    {
        EntityType.Sheep => new Sheep(id, world, position),
        EntityType.Zombie => new Zombie(id, world, position),
        _ => null,
    };
}

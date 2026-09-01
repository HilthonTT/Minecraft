using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

public static class MobFactory
{
    public static Mob? Create(EntityType entityType, int id, World world, Vector3 position) => entityType switch
    {
        EntityType.Sheep => new Sheep(id, world, position),
        EntityType.Pig => new Pig(id, world, position),
        EntityType.Cow => new Cow(id, world, position),
        EntityType.Zombie => new Zombie(id, world, position),
        _ => null,
    };
}

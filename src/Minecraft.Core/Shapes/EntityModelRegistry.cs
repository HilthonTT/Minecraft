using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Textures;
using OpenTK.Mathematics;
using System.Collections.ObjectModel;

namespace Minecraft.Core.Shapes;

public sealed class EntityModelRegistry
{
    // Cells of the block atlas that stand in for entity artwork, which does not exist yet.
    private static readonly Vector2 _dummyCell = new(2, 12);
    private static readonly Vector2 _sheepCell = new(0, 4);
    private static readonly Vector2 _zombieCell = new(5, 3);

    public ReadOnlyDictionary<EntityType, EntityModel> Models { get; }

    public EntityModelRegistry(TextureAtlas textureAtlas)
    {
        var dummy = new BoxEntityModel(textureAtlas, _dummyCell, 0.5F, 2.0F, 0.5F);

        Models = new ReadOnlyDictionary<EntityType, EntityModel>(new Dictionary<EntityType, EntityModel>
        {
            { EntityType.Dummy, dummy },
            { EntityType.Player, dummy },
            {
                EntityType.Sheep,
                new BoxEntityModel(textureAtlas, _sheepCell, Sheep.BodyWidth, Sheep.BodyHeight, Sheep.BodyLength)
            },
            {
                EntityType.Zombie,
                new BoxEntityModel(textureAtlas, _zombieCell, Zombie.BodyWidth, Zombie.BodyHeight, Zombie.BodyLength)
            },
        });
    }
}

using Minecraft.Core.Entities;
using Minecraft.Core.Textures;
using System.Collections.ObjectModel;

namespace Minecraft.Core.Shapes;

public sealed class EntityModelRegistry
{
    public ReadOnlyDictionary<EntityType, EntityModel> Models { get; }

    public EntityModelRegistry(TextureAtlas textureAtlas)
    {
        var dummy = new DummyEntityModel(textureAtlas);

        Models = new ReadOnlyDictionary<EntityType, EntityModel>(new Dictionary<EntityType, EntityModel>
        {
            { EntityType.Dummy, dummy },
            { EntityType.Player, dummy },
        });
    }
}

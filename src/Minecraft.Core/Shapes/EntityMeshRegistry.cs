using Minecraft.Core.Entities;
using Minecraft.Core.Render.MeshGenerator;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using System.Collections.ObjectModel;

namespace Minecraft.Core.Shapes;

/// <summary>
/// Holds the uploaded mesh for each entity type. Meshes are built once at startup, since entity models do
/// not change at runtime.
/// </summary>
public sealed class EntityMeshRegistry
{
    public ReadOnlyDictionary<EntityType, VAOModel> Models { get; }

    public EntityMeshRegistry(TextureAtlas textureAtlas)
    {
        var entityModels = new EntityModelRegistry(textureAtlas);
        var meshGenerator = new EntityMeshGenerator();

        Dictionary<EntityType, VAOModel> registry = [];
        foreach (KeyValuePair<EntityType, EntityModel> entityModel in entityModels.Models)
        {
            registry.Add(entityModel.Key, meshGenerator.GenerateMeshFor(entityModel.Value));
        }

        Models = new ReadOnlyDictionary<EntityType, VAOModel>(registry);
    }
}

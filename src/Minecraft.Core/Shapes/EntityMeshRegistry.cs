using Minecraft.Core.Entities;
using Minecraft.Core.Render.MeshGenerator;
using Minecraft.Core.Render;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;

namespace Minecraft.Core.Shapes;

public readonly record struct EntityMesh(VAOModel Mesh, int SkinTextureId);

[SupportedOSPlatform("windows")]
public sealed class EntityMeshRegistry
{
    public ReadOnlyDictionary<EntityType, EntityMesh> Models { get; }

    public EntityMeshRegistry()
    {
        var entityModels = new EntityModelRegistry();
        var meshGenerator = new EntityMeshGenerator();

        Dictionary<EntityType, EntityMesh> registry = [];
        foreach (KeyValuePair<EntityType, EntityModel> entityModel in entityModels.Models)
        {
            registry.Add(
                entityModel.Key,
                new EntityMesh(meshGenerator.GenerateMeshFor(entityModel.Value), entityModel.Value.Texture.Id));
        }

        Models = new ReadOnlyDictionary<EntityType, EntityMesh>(registry);
    }
}

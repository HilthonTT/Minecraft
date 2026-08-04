using Minecraft.Core.Entities;
using Minecraft.Core.Render.MeshGenerator;
using Minecraft.Core.Utilities;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;

namespace Minecraft.Core.Shapes;

/// <summary>An uploaded entity mesh together with the skin it has to be drawn with.</summary>
/// <param name="Mesh">The geometry, uploaded once at startup.</param>
/// <param name="SkinTextureId">The texture to bind before drawing it.</param>
public readonly record struct EntityMesh(VAOModel Mesh, int SkinTextureId);

/// <summary>
/// Holds the uploaded mesh for each entity type. Meshes are built once at startup, since entity models do
/// not change at runtime.
/// </summary>
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

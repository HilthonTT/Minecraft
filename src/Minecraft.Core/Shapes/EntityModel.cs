using Minecraft.Core.Textures;

namespace Minecraft.Core.Shapes;

/// <summary>
/// The geometry of an entity. Unlike a block model there is no neighbour to hide anything, so every face is
/// always drawn.
/// </summary>
public abstract class EntityModel
{
    /// <summary>The sheet this model's faces are cut from. Entities do not share the block atlas.</summary>
    public Texture Texture { get; }

    public BlockFace[] EntityFaces { get; protected set; } = [];

    protected EntityModel(Texture texture)
    {
        Texture = texture;
    }
}

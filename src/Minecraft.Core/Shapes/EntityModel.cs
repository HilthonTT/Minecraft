using Minecraft.Core.Textures;

namespace Minecraft.Core.Shapes;

/// <summary>
/// The geometry of an entity. Unlike a block model there is no neighbour to hide anything, so every face is
/// always drawn.
/// </summary>
public abstract class EntityModel
{
    protected readonly TextureAtlas _textureAtlas;

    public BlockFace[] EntityFaces { get; protected set; } = [];

    protected EntityModel(TextureAtlas textureAtlas)
    {
        _textureAtlas = textureAtlas;
    }
}

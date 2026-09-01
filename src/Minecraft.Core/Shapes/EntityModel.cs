using Minecraft.Core.Textures;

namespace Minecraft.Core.Shapes;

public abstract class EntityModel
{
    public Texture Texture { get; }

    public BlockFace[] EntityFaces { get; protected set; } = [];

    protected EntityModel(Texture texture)
    {
        Texture = texture;
    }
}

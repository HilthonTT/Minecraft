using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.World.Blocks;
using Vector3i = Minecraft.Core.Utilities.Vector.Vector3i;

namespace Minecraft.Core.Shapes;

public abstract class BlockModel
{
    protected readonly TextureAtlas textureAtlas;
    protected readonly BlockFace[] emptyArray = [];
    protected bool back, right, front, left, top, bottom;
    public bool DoubleSidedFaces { get; protected set; }

    protected BlockModel(TextureAtlas textureAtlas)
    {
        this.textureAtlas = textureAtlas;
    }

    public virtual bool IsOpaqueOnSide(Direction direction)
    {
        return direction switch
        {
            Direction.Back => back,
            Direction.Right => right,
            Direction.Front => front,
            Direction.Left => left,
            Direction.Top => top,
            Direction.Bottom => bottom,
            _ => throw new Exception("Uncatched face."),
        };
    }

    public abstract BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos);
    public abstract BlockFace[] GetPartialVisibleFaces(BlockState state, Vector3i blockPos, Direction direction);
}

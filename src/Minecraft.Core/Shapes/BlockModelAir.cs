using Minecraft.Core.Textures;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// An empty model. Air is never stored in a section, so this is only reached if a lookup goes wrong, where
/// drawing nothing beats throwing on the meshing thread.
/// </summary>
public sealed class BlockModelAir(TextureAtlas textureAtlas) : BlockModel(textureAtlas)
{
    public override bool IsOpaqueOnSide(Direction direction) => false;

    public override BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos) => _emptyArray;

    public override BlockFace[] GetPartialVisibleFaces(BlockState state, Vector3i blockPos, Direction direction) =>
        _emptyArray;
}

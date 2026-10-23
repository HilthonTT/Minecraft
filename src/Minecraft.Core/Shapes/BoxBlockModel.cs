using System.Collections.Concurrent;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// A block drawn as a handful of boxes rather than a whole cube: slabs, stairs, fences, panes, doors and ladders.
/// A shape depends on a small number read off the state — which half, which way it faces, what it joins on to —
/// so each variant is built once and kept, rather than allocated for every block in every chunk remesh.
/// </summary>
public abstract class BoxBlockModel : BlockModel
{
    // Meshing runs off the main thread while held and dropped items are drawn on it, so both may build a variant.
    private readonly ConcurrentDictionary<int, BlockFace[]> _facesByVariant = [];

    protected BoxBlockModel(TextureAtlas textureAtlas) : base(textureAtlas)
    {
        ShadesFacesByDirection = true;
    }

    /// <summary>The boxes for one state. Called once per variant, so it is free to allocate.</summary>
    protected abstract IEnumerable<Box> BoxesFor(BlockState state);

    protected abstract Vector2 CellFor(BlockState state, Direction face);

    public override BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos)
    {
        int variant = state is BlockStateBits bits ? bits.Variant : 0;

        if (_facesByVariant.TryGetValue(variant, out BlockFace[]? faces))
        {
            return faces;
        }

        faces = BoxFaces.Build(_textureAtlas, BoxesFor(state), face => CellFor(state, face));
        return _facesByVariant.GetOrAdd(variant, faces);
    }

    public override BlockFace[] GetPartialVisibleFaces(BlockState state, Vector3i blockPos, Direction direction)
    {
        return _emptyArray;
    }
}

using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.MeshGenerator;

/// <summary>
/// Works out the per vertex light and ambient occlusion values baked into a chunk mesh. Each corner of a
/// face averages the light of the four cells touching it, which is what turns the blocky per block lighting
/// into a smooth gradient and darkens inside corners.
/// </summary>
public sealed class SmoothLighting
{
    /// <summary>Full darkness, used where a neighbour is solid and lets nothing through.</summary>
    private static readonly Light _occluded = new(0, 0, 0, 0, 0);

    /// <summary>
    /// Stand in for a cell in a chunk that is not loaded. Treating it as fully sunlit avoids a dark seam
    /// along the edge of the loaded area.
    /// </summary>
    private static readonly Light _unloaded = new(0, 0, 0, 15, 15);

    private readonly Corner[] _cornerTop = [Corner.BottomLeft, Corner.BottomRight, Corner.TopRight, Corner.TopLeft];
    private readonly Corner[] _cornerBottom = [Corner.TopLeft, Corner.TopRight, Corner.BottomRight, Corner.BottomLeft];
    private readonly Corner[] _cornerRight = [Corner.BottomRight, Corner.BottomLeft, Corner.TopLeft, Corner.TopRight];

    private readonly (Chunk? Chunk, Vector3i Position)[] _blockBuffer = new (Chunk?, Vector3i)[4];
    private readonly Light[] _lightBuffer = new Light[4];
    private readonly Vector3i[] _targetBuffer = new Vector3i[3];

    /// <summary>
    /// The four corner light values for the face of the block at the given chunk local position pointing in
    /// the given direction. The returned array is reused, so consume it before the next call.
    /// </summary>
    public Light[] GetLightsAt(World world, Chunk chunk, int localX, int worldY, int localZ, Direction direction)
    {
        // Light for a face comes from the cell in front of it, not from the block itself.
        Vector3i anchor = new Vector3i(localX, worldY, localZ) + DirectionUtil.ToUnit(direction);

        (Vector3i sourcePos, Chunk sourceChunk) = BlockPropagation.FixReference(world, anchor, chunk, out bool sourceLoaded);

        Block? sourceBlock = null;
        _blockBuffer[0] = sourceLoaded ? (sourceChunk, sourcePos) : (null, sourcePos);
        if (sourceLoaded)
        {
            sourceBlock = sourceChunk.GetBlockAt(sourcePos).GetBlock();
        }

        // The face is buried against a solid block, so nothing reaches it.
        if (sourceLoaded && sourceBlock!.IsOpaque)
        {
            Array.Fill(_lightBuffer, _occluded);
            return _lightBuffer;
        }

        int cornerIndex = 0;
        foreach (Corner corner in GetCornerOrder(direction))
        {
            int bufferIndex = 1;
            foreach (Vector3i target in GetTargets(anchor, direction, corner))
            {
                (Vector3i pos, Chunk targetChunk) = BlockPropagation.FixReference(world, target, chunk, out bool loaded);
                _blockBuffer[bufferIndex] = loaded ? (targetChunk, pos) : (null, pos);
                bufferIndex++;
            }

            _lightBuffer[cornerIndex] = GetCornerLight(sourceLoaded, sourceBlock);
            cornerIndex++;
        }

        return _lightBuffer;
    }

    private Light GetCornerLight(bool sourceLoaded, Block? sourceBlock)
    {
        bool sideOneLoaded = _blockBuffer[1].Chunk is not null;
        bool sideTwoLoaded = _blockBuffer[2].Chunk is not null;
        bool cornerLoaded = _blockBuffer[3].Chunk is not null;

        Block? blockOne = sideOneLoaded ? GetBlockAt(1) : null;
        Block? blockTwo = sideTwoLoaded ? GetBlockAt(2) : null;

        // Both neighbours sharing this corner are solid, so the corner is fully occluded and takes the
        // light of the cell in front of the face rather than an average that light cannot reach.
        if (sideOneLoaded && sideTwoLoaded && blockOne!.IsOpaque && blockTwo!.IsOpaque)
        {
            if (!sourceLoaded)
            {
                return new Light(0, 10, 0, 0, 1);
            }

            (Chunk? currentChunk, Vector3i position) = _blockBuffer[0];
            uint x = (uint)position.X;
            uint y = (uint)position.Y;
            uint z = (uint)position.Z;

            return new Light(
                currentChunk!.LightMap.GetRedBlockLightAt(x, y, z),
                currentChunk.LightMap.GetGreenBlockLightAt(x, y, z),
                currentChunk.LightMap.GetBlueBlockLightAt(x, y, z),
                currentChunk.LightMap.GetSunLightIntensityAt(x, y, z),
                54);
        }

        Block? blockCorner = cornerLoaded ? GetBlockAt(3) : null;

        Light lightSource = SampleLight(0, sourceLoaded, sourceBlock);
        Light lightOne = SampleLight(1, sideOneLoaded, blockOne);
        Light lightTwo = SampleLight(2, sideTwoLoaded, blockTwo);
        Light lightCorner = SampleLight(3, cornerLoaded, blockCorner);

        // The channels are summed rather than averaged; the shader divides back down by the brightness.
        return new Light(
            lightSource.GetRedChannel() + lightOne.GetRedChannel() + lightTwo.GetRedChannel() + lightCorner.GetRedChannel(),
            lightSource.GetGreenChannel() + lightOne.GetGreenChannel() + lightTwo.GetGreenChannel() + lightCorner.GetGreenChannel(),
            lightSource.GetBlueChannel() + lightOne.GetBlueChannel() + lightTwo.GetBlueChannel() + lightCorner.GetBlueChannel(),
            lightSource.GetSunlight() + lightOne.GetSunlight() + lightTwo.GetSunlight() + lightCorner.GetSunlight(),
            63);
    }

    private Block GetBlockAt(int bufferIndex)
    {
        (Chunk? chunk, Vector3i position) = _blockBuffer[bufferIndex];
        return chunk!.GetBlockAt(position).GetBlock();
    }

    private Light SampleLight(int bufferIndex, bool loaded, Block? block)
    {
        if (!loaded)
        {
            return _unloaded;
        }

        if (block!.IsOpaque)
        {
            return _occluded;
        }

        (Chunk? chunk, Vector3i position) = _blockBuffer[bufferIndex];
        return chunk!.LightMap.GetLightColorAt(position);
    }

    private Corner[] GetCornerOrder(Direction direction)
    {
        return direction switch
        {
            Direction.Top => _cornerTop,
            Direction.Bottom => _cornerBottom,
            Direction.Left => _cornerTop,
            Direction.Right => _cornerRight,
            Direction.Front => _cornerTop,
            Direction.Back => _cornerRight,
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
    }

    /// <summary>
    /// The three cells that share the given corner with the anchor cell: the two edge neighbours and the
    /// diagonal one. Which axes those lie on depends on which way the face points.
    /// </summary>
    private Vector3i[] GetTargets(Vector3i anchor, Direction direction, Corner corner)
    {
        // The two axes that span the face plane. The corner naming lines up with these two axes the same
        // way for every direction, so the sign mapping below is shared.
        (Vector3i first, Vector3i second) = direction switch
        {
            Direction.Top or Direction.Bottom => (new Vector3i(1, 0, 0), new Vector3i(0, 0, 1)),
            Direction.Left or Direction.Right => (new Vector3i(0, 1, 0), new Vector3i(0, 0, 1)),
            Direction.Front or Direction.Back => (new Vector3i(0, 1, 0), new Vector3i(1, 0, 0)),
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };

        (int firstSign, int secondSign) = corner switch
        {
            Corner.TopLeft => (1, -1),
            Corner.TopRight => (1, 1),
            Corner.BottomLeft => (-1, -1),
            Corner.BottomRight => (-1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(corner)),
        };

        _targetBuffer[0] = anchor + first * firstSign;
        _targetBuffer[1] = anchor + second * secondSign;
        _targetBuffer[2] = anchor + first * firstSign + second * secondSign;
        return _targetBuffer;
    }
}

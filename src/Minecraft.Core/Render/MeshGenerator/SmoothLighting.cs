using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.MeshGenerator;

public sealed class SmoothLighting
{
    private static readonly Light _occluded = new(0, 0, 0, 0, 0);

    private static readonly Light _unloaded = new(0, 0, 0, 15, 15);

    private readonly Corner[] _cornerTop = [Corner.BottomLeft, Corner.BottomRight, Corner.TopRight, Corner.TopLeft];
    private readonly Corner[] _cornerBottom = [Corner.TopLeft, Corner.TopRight, Corner.BottomRight, Corner.BottomLeft];
    private readonly Corner[] _cornerRight = [Corner.BottomRight, Corner.BottomLeft, Corner.TopLeft, Corner.TopRight];

    private readonly (Chunk? Chunk, Vector3i Position)[] _blockBuffer = new (Chunk?, Vector3i)[4];
    private readonly Light[] _lightBuffer = new Light[4];
    private readonly Vector3i[] _targetBuffer = new Vector3i[3];

    public Light[] GetLightsAt(World world, Chunk chunk, int localX, int worldY, int localZ, Direction direction)
    {
        Vector3i anchor = new Vector3i(localX, worldY, localZ) + DirectionUtil.ToUnit(direction);

        (Vector3i sourcePos, Chunk sourceChunk) = BlockPropagation.FixReference(world, anchor, chunk, out bool sourceLoaded);

        Block? sourceBlock = null;
        _blockBuffer[0] = sourceLoaded ? (sourceChunk, sourcePos) : (null, sourcePos);
        if (sourceLoaded)
        {
            sourceBlock = sourceChunk.GetBlockAt(sourcePos).GetBlock();
        }

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

    private Vector3i[] GetTargets(Vector3i anchor, Direction direction, Corner corner)
    {
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

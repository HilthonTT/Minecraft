using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using Minecraft.Core.Worlds.Sections;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.MeshGenerator;

/// <summary>
/// Meshes the solid geometry of a chunk. A face is only emitted when the neighbour on that side does not
/// cover it, which is what keeps the interior of the terrain out of the vertex buffer entirely.
/// </summary>
public sealed class OpaqueMeshGenerator : MeshGenerator
{
    // Faces are shaded by orientation so that a flat lit scene still reads as three dimensional.
    private const uint StaticTopLight = 60;
    private const uint StaticBottomLight = 36;
    private const uint StaticSideXLight = 52;
    private const uint StaticSideZLight = 44;

    /// <summary>
    /// Lightmap values are 0..15 while the packed light channels are 0..63, so samples are scaled up on
    /// the way into a vertex.
    /// </summary>
    private const uint LightScale = 4;

    private readonly SmoothLighting _smoothLighting = new();
    private readonly bool _useSmoothLighting = true;
    private readonly Light[] _lightBuffer = new Light[4];

    public OpaqueMeshGenerator(BlockModelRegistry blockModelRegistry) : base(blockModelRegistry)
    {
    }

    protected override ChunkBufferLayout GenerateMesh(World world, Chunk chunk)
    {
        world.LoadedChunks.TryGetValue(new Vector2(chunk.GridX - 1, chunk.GridZ), out Chunk? chunkXNeg);
        world.LoadedChunks.TryGetValue(new Vector2(chunk.GridX + 1, chunk.GridZ), out Chunk? chunkXPos);
        world.LoadedChunks.TryGetValue(new Vector2(chunk.GridX, chunk.GridZ - 1), out Chunk? chunkZNeg);
        world.LoadedChunks.TryGetValue(new Vector2(chunk.GridX, chunk.GridZ + 1), out Chunk? chunkZPos);

        for (int sectionHeight = 0; sectionHeight < chunk.Sections.Length; sectionHeight++)
        {
            Section? section = chunk.Sections[sectionHeight];
            if (section == null || section.IsEmpty)
            {
                continue;
            }

            for (int localX = 0; localX < 16; localX++)
            {
                for (int localZ = 0; localZ < 16; localZ++)
                {
                    for (int sectionLocalY = 0; sectionLocalY < 16; sectionLocalY++)
                    {
                        BlockState? state = section.GetBlockAt(localX, sectionLocalY, localZ);
                        if (state == null)
                        {
                            continue;
                        }

                        MeshBlock(
                            world,
                            chunk,
                            section,
                            state,
                            localX,
                            sectionLocalY,
                            sectionHeight,
                            localZ,
                            chunkXNeg,
                            chunkXPos,
                            chunkZNeg,
                            chunkZPos);
                    }
                }
            }
        }

        return new ChunkBufferLayout
        {
            VertexPositions = _vertexPositions,
            PositionsPointer = _positionPointer,
            VertexUVs = _vertexUVs,
            UVsPointer = _uvsPointer,
            VertexLights = _vertexLights,
            LightsPointer = _lightsPointer,
            VertexNormals = _vertexNormals,
            NormalsPointer = _normalPointer,
            IndicesCount = _indicesCount,
        };
    }

    private void MeshBlock(
        World world,
        Chunk chunk,
        Section section,
        BlockState state,
        int localX,
        int sectionLocalY,
        int sectionHeight,
        int localZ,
        Chunk? chunkXNeg,
        Chunk? chunkXPos,
        Chunk? chunkZNeg,
        Chunk? chunkZPos)
    {
        BlockModel blockModel = _blockModelRegistry.Models[state.GetBlock().Id];

        int worldY = sectionLocalY + sectionHeight * 16;
        var chunkLocalPos = new Vector3i(localX, worldY, localZ);
        var worldPos = new Vector3i(localX + chunk.GridX * 16, worldY, localZ + chunk.GridZ * 16);

        // Faces that are always drawn have no neighbour of their own to take light from, so they average
        // the light of whichever sides did get drawn.
        uint averageRed = 0;
        uint averageGreen = 0;
        uint averageBlue = 0;
        int sampleCount = 0;

        if (ShouldAddFace(chunkXPos, section, localX + 1, sectionLocalY, localZ, Direction.Left))
        {
            Light light = SampleNeighbourLight(chunk, chunkXPos, chunkLocalPos, 1, 0, StaticSideZLight);
            Accumulate(light, ref averageRed, ref averageGreen, ref averageBlue, ref sampleCount);
            BuildMeshForSide(world, chunk, Direction.Right, state, chunkLocalPos, worldPos, blockModel, light);
        }

        if (ShouldAddFace(chunkXNeg, section, localX - 1, sectionLocalY, localZ, Direction.Right))
        {
            Light light = SampleNeighbourLight(chunk, chunkXNeg, chunkLocalPos, -1, 0, StaticSideZLight);
            Accumulate(light, ref averageRed, ref averageGreen, ref averageBlue, ref sampleCount);
            BuildMeshForSide(world, chunk, Direction.Left, state, chunkLocalPos, worldPos, blockModel, light);
        }

        if (ShouldAddFace(chunkZNeg, section, localX, sectionLocalY, localZ - 1, Direction.Front))
        {
            Light light = SampleNeighbourLight(chunk, chunkZNeg, chunkLocalPos, 0, -1, StaticSideXLight);
            Accumulate(light, ref averageRed, ref averageGreen, ref averageBlue, ref sampleCount);
            BuildMeshForSide(world, chunk, Direction.Back, state, chunkLocalPos, worldPos, blockModel, light);
        }

        if (ShouldAddFace(chunkZPos, section, localX, sectionLocalY, localZ + 1, Direction.Back))
        {
            Light light = SampleNeighbourLight(chunk, chunkZPos, chunkLocalPos, 0, 1, StaticSideXLight);
            Accumulate(light, ref averageRed, ref averageGreen, ref averageBlue, ref sampleCount);
            BuildMeshForSide(world, chunk, Direction.Front, state, chunkLocalPos, worldPos, blockModel, light);
        }

        if (ShouldAddVerticalFace(chunk, section, localX, sectionLocalY, localZ, above: true))
        {
            uint sampleY = (uint)Math.Min(worldY + 1, Constants.MAX_BUILD_HEIGHT - 1);
            Light light = chunk.LightMap.GetLightColorAt((uint)localX, sampleY, (uint)localZ, LightScale);
            light.SetBrightness(StaticTopLight);
            Accumulate(light, ref averageRed, ref averageGreen, ref averageBlue, ref sampleCount);
            BuildMeshForSide(world, chunk, Direction.Top, state, chunkLocalPos, worldPos, blockModel, light);
        }

        if (ShouldAddVerticalFace(chunk, section, localX, sectionLocalY, localZ, above: false))
        {
            uint sampleY = (uint)Math.Max(worldY - 1, 0);
            Light light = chunk.LightMap.GetLightColorAt((uint)localX, sampleY, (uint)localZ, LightScale);
            light.SetBrightness(StaticBottomLight);
            Accumulate(light, ref averageRed, ref averageGreen, ref averageBlue, ref sampleCount);
            BuildMeshForSide(world, chunk, Direction.Bottom, state, chunkLocalPos, worldPos, blockModel, light);
        }

        BlockFace[] alwaysVisibleFaces = blockModel.GetAlwaysVisibleFaces(state, worldPos);
        if (alwaysVisibleFaces.Length == 0)
        {
            return;
        }

        var alwaysVisibleLight = new Light();
        if (!state.GetBlock().IsOpaque)
        {
            alwaysVisibleLight = chunk.LightMap.GetLightColorAt(
                (uint)chunkLocalPos.X,
                (uint)chunkLocalPos.Y,
                (uint)chunkLocalPos.Z,
                LightScale);
        }
        else if (sampleCount != 0)
        {
            alwaysVisibleLight.SetRedChannel(Math.Min(averageRed / (uint)sampleCount, Light.MaxChannelValue));
            alwaysVisibleLight.SetGreenChannel(Math.Min(averageGreen / (uint)sampleCount, Light.MaxChannelValue));
            alwaysVisibleLight.SetBlueChannel(Math.Min(averageBlue / (uint)sampleCount, Light.MaxChannelValue));
        }

        alwaysVisibleLight.SetSunlight(
            chunk.LightMap.GetSunLightIntensityAt((uint)chunkLocalPos.X, (uint)chunkLocalPos.Y, (uint)chunkLocalPos.Z)
            * LightScale);
        alwaysVisibleLight.SetBrightness(Light.MaxChannelValue);

        Array.Fill(_lightBuffer, alwaysVisibleLight);

        if (blockModel.DoubleSidedFaces)
        {
            AddFacesToMeshDualSided(alwaysVisibleFaces, chunkLocalPos, _lightBuffer, false);
        }
        else
        {
            AddFacesToMeshFromFront(alwaysVisibleFaces, chunkLocalPos, _lightBuffer, false);
        }
    }

    private static void Accumulate(Light light, ref uint red, ref uint green, ref uint blue, ref int count)
    {
        red += light.GetRedChannel();
        green += light.GetGreenChannel();
        blue += light.GetBlueChannel();
        count++;
    }

    /// <summary>
    /// Reads the lightmap of the cell one step along the horizontal offset, crossing into the neighbouring
    /// chunk when the offset leaves this one.
    /// </summary>
    private static Light SampleNeighbourLight(
        Chunk chunk,
        Chunk? neighbourChunk,
        Vector3i chunkLocalPos,
        int offsetX,
        int offsetZ,
        uint brightness)
    {
        int x = chunkLocalPos.X + offsetX;
        int z = chunkLocalPos.Z + offsetZ;

        Light light = new();
        if (x is >= 0 and <= 15 && z is >= 0 and <= 15)
        {
            light = chunk.LightMap.GetLightColorAt((uint)x, (uint)chunkLocalPos.Y, (uint)z, LightScale);
        }
        else if (neighbourChunk != null)
        {
            light = neighbourChunk.LightMap.GetLightColorAt(
                (uint)(x & 15),
                (uint)chunkLocalPos.Y,
                (uint)(z & 15),
                LightScale);
        }

        light.SetBrightness(brightness);
        return light;
    }

    private void BuildMeshForSide(
        World world,
        Chunk chunk,
        Direction direction,
        BlockState state,
        Vector3i chunkLocalPos,
        Vector3i worldPos,
        BlockModel model,
        Light flatLight)
    {
        Light[] lights;
        if (_useSmoothLighting)
        {
            lights = _smoothLighting.GetLightsAt(
                world,
                chunk,
                chunkLocalPos.X,
                chunkLocalPos.Y,
                chunkLocalPos.Z,
                direction);
        }
        else
        {
            Array.Fill(_lightBuffer, flatLight);
            lights = _lightBuffer;
        }

        BlockFace[] faces = model.GetPartialVisibleFaces(state, worldPos, direction);
        AddFacesToMeshFromFront(faces, chunkLocalPos, lights, ShouldFlipTriangles(lights));
    }

    /// <summary>
    /// Picks the diagonal that splits the quad along the flatter light gradient. Without this a quad with
    /// one dark corner shows a hard triangular seam instead of a smooth falloff.
    /// </summary>
    private static bool ShouldFlipTriangles(Light[] lights)
    {
        (uint red1, uint green1, uint blue1, uint sun1, uint _) = Light.Add(lights[0], lights[2]);
        (uint red2, uint green2, uint blue2, uint sun2, uint _) = Light.Add(lights[1], lights[3]);
        return red1 + green1 + blue1 + sun1 > red2 + green2 + blue2 + sun2;
    }

    /// <summary>
    /// Whether the face towards the given neighbouring cell needs drawing. The coordinates may leave the
    /// current chunk, in which case the neighbouring chunk is consulted instead.
    /// </summary>
    private bool ShouldAddFace(
        Chunk? neighbourChunk,
        Section currentSection,
        int localX,
        int localY,
        int localZ,
        Direction facingBack)
    {
        BlockState? neighbour;

        if (localX is >= 0 and <= 15 && localZ is >= 0 and <= 15)
        {
            neighbour = currentSection.GetBlockAt(localX, localY, localZ);
        }
        else
        {
            Section? neighbourSection = neighbourChunk?.Sections[currentSection.Height];
            if (neighbourSection == null)
            {
                return true;
            }

            neighbour = neighbourSection.GetBlockAt(localX & 15, localY, localZ & 15);
        }

        if (neighbour == null)
        {
            return true;
        }

        return !_blockModelRegistry.Models[neighbour.GetBlock().Id].IsOpaqueOnSide(facingBack);
    }

    /// <summary>
    /// Whether the top or bottom face needs drawing. Vertical neighbours may live in the section above or
    /// below rather than in a neighbouring chunk.
    /// </summary>
    private bool ShouldAddVerticalFace(Chunk chunk, Section currentSection, int localX, int localY, int localZ, bool above)
    {
        int neighbourY = above ? localY + 1 : localY - 1;
        BlockState? neighbour;

        if (neighbourY is >= 0 and <= 15)
        {
            neighbour = currentSection.GetBlockAt(localX, neighbourY, localZ);
        }
        else
        {
            int neighbourSectionHeight = above ? currentSection.Height + 1 : currentSection.Height - 1;
            if (neighbourSectionHeight < 0 || neighbourSectionHeight >= Constants.NUM_SECTIONS_IN_CHUNKS)
            {
                return true;
            }

            Section? neighbourSection = chunk.Sections[neighbourSectionHeight];
            if (neighbourSection == null)
            {
                return true;
            }

            neighbour = neighbourSection.GetBlockAt(localX, above ? 0 : 15, localZ);
        }

        if (neighbour == null)
        {
            return true;
        }

        Direction facingBack = above ? Direction.Bottom : Direction.Top;
        return !_blockModelRegistry.Models[neighbour.GetBlock().Id].IsOpaqueOnSide(facingBack);
    }
}

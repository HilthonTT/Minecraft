using Minecraft.Core.Render;
using Minecraft.Core.Render.MeshGenerator;

namespace Minecraft.Tests.Render;

public sealed class MeshBuffersTests
{
    [Fact]
    public void BuffersGrowInsteadOfOverflowing()
    {
        var buffers = new MeshBuffers(6);

        for (int vertex = 0; vertex < 100; vertex++)
        {
            buffers.EnsureRoomForVertices(1);
            buffers.Positions[buffers.PositionsPointer++] = vertex;
            buffers.Positions[buffers.PositionsPointer++] = vertex;
            buffers.Positions[buffers.PositionsPointer++] = vertex;
            buffers.UVs[buffers.UVsPointer++] = vertex;
            buffers.UVs[buffers.UVsPointer++] = vertex;
            buffers.Lights[buffers.LightsPointer++] = (uint)vertex;
            buffers.Normals[buffers.NormalsPointer++] = vertex;
            buffers.Normals[buffers.NormalsPointer++] = vertex;
            buffers.Normals[buffers.NormalsPointer++] = vertex;
            buffers.IndicesCount++;
        }

        ChunkBufferLayout layout = buffers.ToLayout();

        Assert.Equal(300, layout.PositionsPointer);
        Assert.Equal(100, layout.IndicesCount);
        Assert.True(layout.VertexPositions.Length >= 300);
        Assert.Equal(0F, layout.VertexPositions[0]);
        Assert.Equal(99F, layout.VertexPositions[299]);
        Assert.Equal(99u, layout.VertexLights[99]);
    }

    [Fact]
    public void ALayoutKeepsItsVerticesWhenTheBuffersAreReused()
    {
        var buffers = new MeshBuffers(3);
        buffers.EnsureRoomForVertices(1);
        buffers.Positions[buffers.PositionsPointer++] = 7F;
        buffers.Lights[buffers.LightsPointer++] = 7u;
        buffers.IndicesCount++;

        ChunkBufferLayout layout = buffers.ToLayout();

        buffers.Clear();
        buffers.EnsureRoomForVertices(1);
        buffers.Positions[buffers.PositionsPointer++] = 99F;
        buffers.Lights[buffers.LightsPointer++] = 99u;

        Assert.Equal(7F, layout.VertexPositions[0]);
        Assert.Equal(7u, layout.VertexLights[0]);
        Assert.Equal(layout.PositionsPointer, layout.VertexPositions.Length);
    }

    [Fact]
    public void ClearingKeepsTheGrownCapacity()
    {
        var buffers = new MeshBuffers(3);
        buffers.EnsureRoomForVertices(10);
        int grownCapacity = buffers.Positions.Length;

        buffers.Clear();

        Assert.True(grownCapacity >= 30);
        Assert.Equal(grownCapacity, buffers.Positions.Length);
        Assert.Equal(0, buffers.PositionsPointer);
    }
}

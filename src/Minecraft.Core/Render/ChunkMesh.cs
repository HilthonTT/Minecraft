namespace Minecraft.Core.Render;

/// <summary>
/// The finished geometry of one chunk, split by how it has to be drawn. The solid blocks go down first with
/// depth writes on; the water is drawn afterwards, blended over whatever ended up behind it.
/// </summary>
public struct ChunkMesh
{
    public ChunkBufferLayout Opaque;
    public ChunkBufferLayout Liquid;
}

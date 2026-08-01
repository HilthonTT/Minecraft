using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Lighting;

public static class LightUtils
{
    /// <summary>
    /// The three color channels (R, G, B) in which block light propagates.
    /// </summary>
    public readonly static LightChannel[] BlockVisibileColorChannels = [LightChannel.Red, LightChannel.Green, LightChannel.Blue];

    /// <summary>
    /// Returns the color of the given channel from the given light source
    /// </summary>
    public static uint GetChannelColor(ILightSource source, LightChannel channel)
    {
        return channel switch
        {
            LightChannel.Red => (uint)source.LightColor.X,
            LightChannel.Green => (uint)source.LightColor.Y,
            LightChannel.Blue => (uint)source.LightColor.Z,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    /// <summary>
    /// Returns the color of the given channel at the given local position in the chunk's lightmap.
    /// </summary>
    public static uint GetLightOfChannel(Chunk chunk, Vector3i chunkLocalPos, LightChannel channel)
    {
        return channel switch
        {
            LightChannel.Red => chunk.LightMap.GetRedBlockLightAt(chunkLocalPos),
            LightChannel.Green => chunk.LightMap.GetGreenBlockLightAt(chunkLocalPos),
            LightChannel.Blue => chunk.LightMap.GetBlueBlockLightAt(chunkLocalPos),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    /// <summary>
    /// Sets the color of the given channel at the given location in the chunk's lightmap.
    /// </summary>
    public static void SetLightOfChannel(Chunk chunk, Vector3i chunkLocalPos, LightChannel channel, uint value)
    {
        switch (channel)
        {
            case LightChannel.Red:
                chunk.LightMap.SetRedBlockLightAt(chunkLocalPos, value);
                break;
            case LightChannel.Green:
                chunk.LightMap.SetGreenBlockLightAt(chunkLocalPos, value);
                break;
            case LightChannel.Blue:
                chunk.LightMap.SetBlueBlockLightAt(chunkLocalPos, value);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

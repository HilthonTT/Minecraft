using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Lighting;

public static class LightUtils
{
    public readonly static LightChannel[] BlockVisibileColorChannels = [LightChannel.Red, LightChannel.Green, LightChannel.Blue];

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

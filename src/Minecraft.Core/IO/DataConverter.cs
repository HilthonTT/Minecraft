using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using System.Runtime.CompilerServices;
using System.Text;
using OpenTK.Mathematics;
using Minecraft.Core.Worlds;

namespace Minecraft.Core.IO;

public static class DataConverter
{
    private static readonly UTF8Encoding Utf8 = new();

    public static byte[] StringUtf8ToBytes(string value)
    {
        return Utf8.GetBytes(value);
    }

    public static string BytesToUtf8String(byte[] bytes)
    {
        return Utf8.GetString(bytes);
    }

    public static ushort BytesToUInt16(byte[] bytes, ref int head)
    {
        //Little endian
        ushort value = (ushort)(bytes[head] | (bytes[head + 1] << 8));
        head += 2;
        return value;
    }

    public static int BytesToInt32(byte[] bytes, ref int head)
    {
        //Little endian
        int value = bytes[head] | (bytes[head + 1] << 8) | (bytes[head + 2] << 16) | (bytes[head + 3] << 24);
        head += 4;
        return value;
    }

    public static unsafe float BytesToFloat(byte[] bytes, ref int head)
    {
        //Little endian
        int value = BytesToInt32(bytes, ref head);
        return *(float*)&value;
    }

    public static bool BytesToBool(byte[] bytes, ref int head)
    {
        bool value = bytes[head] != 0;
        head += 1;
        return value;
    }

    /// <summary>Reads a single byte and reinterprets it as <typeparamref name="T"/>, typically a byte backed enum.</summary>
    public static unsafe T BytesToByteStruct<T>(byte[] bytes, ref int head) where T : unmanaged
    {
        // Boxing the byte and unboxing it as T would throw for every type but byte itself,
        // including the byte backed enums this is meant to read.
        if (sizeof(T) != 1)
        {
            throw new NotSupportedException($"{typeof(T).Name} is not a single byte type.");
        }

        byte value = bytes[head++];
        return Unsafe.As<byte, T>(ref value);
    }

    public static Vector3i BytesToVector3i(byte[] bytes, ref int head)
    {
        int x = BytesToInt32(bytes, ref head);
        int y = BytesToInt32(bytes, ref head);
        int z = BytesToInt32(bytes, ref head);
        return new Vector3i(x, y, z);
    }

    public static Chunk BytesToChunk(byte[] bytes, World world, ref int head)
    {
        ArgumentNullException.ThrowIfNull(world);

        int gridX = BytesToInt32(bytes, ref head);
        int gridZ = BytesToInt32(bytes, ref head);

        Chunk chunk = world.ChunkPool.GetObject();
        chunk.ResetAndAssign(gridX, gridZ);

        for (int i = 0; i < Constants.NUM_SECTIONS_IN_CHUNKS; i++)
        {
            bool doesSectionHaveBlocks = BytesToBool(bytes, ref head);
            if (doesSectionHaveBlocks)
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            ushort blockId = BytesToUInt16(bytes, ref head);
                            if (blockId != 0)
                            {
                                BlockState blockState = BlockRegistry.GetState(BlockRegistry.GetBlockFromIdentifier(blockId));
                                blockState.ExtractFromByteStream(bytes, ref head);
                                chunk.AddBlockAt(x, i * 16 + y, z, blockState);
                            }
                        }
                    }
                }
            }
        }
        return chunk;
    }
}

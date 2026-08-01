using Minecraft.Core.Logging;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Sections;
using OpenTK.Mathematics;

namespace Minecraft.Core.IO;

public sealed class BufferedDataStream
{
    private readonly BufferedStream _bufferedStream;

    public BufferedDataStream(BufferedStream bufferedStream)
    {
        _bufferedStream = bufferedStream;
    }

    public bool Flush()
    {
        try
        {
            _bufferedStream.Flush();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Flushing failed: " + ex.Message);
            return false;
        }
    }

    public unsafe void WriteInt32(int value)
    {
        byte* pValue = (byte*)&value;
        _bufferedStream.WriteByte(pValue[0]);
        _bufferedStream.WriteByte(pValue[1]);
        _bufferedStream.WriteByte(pValue[2]);
        _bufferedStream.WriteByte(pValue[3]);
    }

    public unsafe void WriteFloat(float value)
    {
        WriteInt32(*(int*)&value);
    }

    public void WriteByte(byte value)
    {
        _bufferedStream.WriteByte(value);
    }

    public unsafe void WriteBool(bool value)
    {
        _bufferedStream.WriteByte(((byte*)&value)[0]);
    }

    public unsafe void WriteInt16(short value)
    {
        byte* pValue = (byte*)&value;
        _bufferedStream.WriteByte(pValue[0]);
        _bufferedStream.WriteByte(pValue[1]);
    }

    public unsafe void WriteUInt16(ushort value)
    {
        byte* pValue = (byte*)&value;
        _bufferedStream.WriteByte(pValue[0]);
        _bufferedStream.WriteByte(pValue[1]);
    }

    public void WriteUtf8String(string value)
    {
        byte[] messageBytes = DataConverter.StringUtf8ToBytes(value);
        WriteInt32(messageBytes.Length);
        _bufferedStream.Write(messageBytes, 0, messageBytes.Length);
    }

    public void WriteVector3i(Vector3i value)
    {
        WriteInt32(value.X);
        WriteInt32(value.Y);
        WriteInt32(value.Z);
    }

    public void WriteVector3(Vector3 value)
    {
        WriteFloat(value.X);
        WriteFloat(value.Y);
        WriteFloat(value.Z);
    }

    public void WriteBytes(byte[] value)
    {
        _bufferedStream.Write(value, 0, value.Length);
    }

    public void WriteChunk(Chunk value)
    {
        WriteInt32(value.GetPayloadSize() + sizeof(int) + sizeof(int));
        WriteInt32(value.GridX);
        WriteInt32(value.GridZ);

        for (int i = 0; i < Constants.NUM_SECTIONS_IN_CHUNKS; i++)
        {
            Section? section = value.Sections[i];
            if (section is null)
            {
                WriteBool(false);
            }
            else
            {
                WriteBool(true);
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            BlockState? state = section.GetBlockAt(x, y, z);
                            if (state is null)
                            {
                                WriteUInt16(0);
                            }
                            else
                            {
                                state.ToStream(this);
                            }
                        }
                    }
                }
            }
        }
    }
}

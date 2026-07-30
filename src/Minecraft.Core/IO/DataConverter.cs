using OpenTK.Mathematics;
using System.Text;

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
        bool value = bytes[head] == 0 ? false : true;
        head += 1;
        return value;
    }

    public static T BytesToByteStruct<T>(byte[] bytes, ref int head) where T : struct
    {
        return (T)(object)bytes[head++];
    }

    public static Vector3i BytesToVector3i(byte[] bytes, ref int head)
    {
        int x = BytesToInt32(bytes, ref head);
        int y = BytesToInt32(bytes, ref head);
        int z = BytesToInt32(bytes, ref head);
        return new Vector3i(x, y, z);
    }
}

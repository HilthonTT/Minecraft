using Minecraft.Core.Games;
using Minecraft.Core.IO;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Network;

public sealed class DataConverterTests
{
    private static byte[] Written(Action<BufferedDataStream> write)
    {
        using var memory = new MemoryStream();
        using (var buffered = new BufferedStream(memory))
        {
            write(new BufferedDataStream(buffered));
            buffered.Flush();
        }

        return memory.ToArray();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void AnInt32SurvivesTheRoundTrip(int value)
    {
        byte[] bytes = Written(writer => writer.WriteInt32(value));
        int head = 0;

        Assert.Equal(4, bytes.Length);
        Assert.Equal(value, DataConverter.BytesToInt32(bytes, ref head));
        Assert.Equal(4, head);
    }

    [Theory]
    [InlineData(0F)]
    [InlineData(0.5F)]
    [InlineData(-123.456F)]
    [InlineData(float.MaxValue)]
    public void AFloatSurvivesTheRoundTrip(float value)
    {
        byte[] bytes = Written(writer => writer.WriteFloat(value));
        int head = 0;

        Assert.Equal(value, DataConverter.BytesToFloat(bytes, ref head));
        Assert.Equal(4, head);
    }

    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)46)]
    [InlineData(ushort.MaxValue)]
    public void AUInt16SurvivesTheRoundTrip(ushort value)
    {
        byte[] bytes = Written(writer => writer.WriteUInt16(value));
        int head = 0;

        Assert.Equal(2, bytes.Length);
        Assert.Equal(value, DataConverter.BytesToUInt16(bytes, ref head));
        Assert.Equal(2, head);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABoolSurvivesTheRoundTrip(bool value)
    {
        byte[] bytes = Written(writer => writer.WriteBool(value));
        int head = 0;

        Assert.Single(bytes);
        Assert.Equal(value, DataConverter.BytesToBool(bytes, ref head));
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("a message with spaces")]
    [InlineData("unicode: éü世界")]
    public void AStringSurvivesTheRoundTripWithItsLengthInFront(string value)
    {
        byte[] bytes = Written(writer => writer.WriteUtf8String(value));
        int head = 0;

        int byteCount = DataConverter.BytesToInt32(bytes, ref head);

        Assert.Equal(bytes.Length - sizeof(int), byteCount);
        Assert.Equal(value, DataConverter.BytesToUtf8String(bytes[head..]));
    }

    [Fact]
    public void AVectorSurvivesTheRoundTrip()
    {
        var position = new Vector3i(1, -2000, 3);
        byte[] bytes = Written(writer => writer.WriteVector3i(position));
        int head = 0;

        Assert.Equal(position, DataConverter.BytesToVector3i(bytes, ref head));
        Assert.Equal(12, head);
    }

    [Fact]
    public void ManyValuesAreReadBackInTheOrderTheyWereWritten()
    {
        byte[] bytes = Written(writer =>
        {
            writer.WriteInt32(7);
            writer.WriteBool(true);
            writer.WriteUInt16(300);
            writer.WriteFloat(1.5F);
        });

        int head = 0;

        Assert.Equal(7, DataConverter.BytesToInt32(bytes, ref head));
        Assert.True(DataConverter.BytesToBool(bytes, ref head));
        Assert.Equal(300, DataConverter.BytesToUInt16(bytes, ref head));
        Assert.Equal(1.5F, DataConverter.BytesToFloat(bytes, ref head));
        Assert.Equal(bytes.Length, head);
    }

    [Fact]
    public void AByteBackedEnumIsReadAsItself()
    {
        byte[] bytes = [1, 0];
        int head = 0;

        Assert.Equal(SingleByte.Second, DataConverter.BytesToByteStruct<SingleByte>(bytes, ref head));
        Assert.Equal(SingleByte.First, DataConverter.BytesToByteStruct<SingleByte>(bytes, ref head));
        Assert.Equal(2, head);
    }

    [Fact]
    public void AnythingWiderThanAByteIsRefusedRatherThanMisread()
    {
        byte[] bytes = [1, 0, 0, 0];
        int head = 0;

        Assert.Throws<NotSupportedException>(() => DataConverter.BytesToByteStruct<GameMode>(bytes, ref head));
    }

    private enum SingleByte : byte
    {
        First,
        Second,
    }
}

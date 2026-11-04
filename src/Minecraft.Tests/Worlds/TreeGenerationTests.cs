using Minecraft.Core;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Decoration.Trees;

namespace Minecraft.Tests.Worlds;

[Collection(RegistryCollection.Name)]
public sealed class TreeGenerationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AnOakWithNoRoomUnderTheBuildLimitIsNotGrown(int seed)
    {
        var chunk = new Chunk(0, 0);

        new OakTreeGenerator().GenerateTreeAt(chunk, 8, Constants.MAX_BUILD_HEIGHT - 2, 8, new Random(seed));

        Assert.Equal(BlockRegistry.Air, chunk.GetBlockAt(8, Constants.MAX_BUILD_HEIGHT - 2, 8).GetBlock());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AnOakThatJustFitsUnderTheBuildLimitIsGrown(int seed)
    {
        var chunk = new Chunk(0, 0);
        int groundY = Constants.MAX_BUILD_HEIGHT - 8;

        new OakTreeGenerator().GenerateTreeAt(chunk, 8, groundY, 8, new Random(seed));

        Assert.Equal(BlockRegistry.OakLog, chunk.GetBlockAt(8, groundY, 8).GetBlock());
    }
}

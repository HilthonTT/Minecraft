using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Storage;

namespace Minecraft.Tests.Worlds;

[Collection(RegistryCollection.Name)]
public sealed class ChunkStoreTests : IDisposable
{
    private readonly string _chunkDirectory =
        Path.Combine(Path.GetTempPath(), "minecraft-tests", Path.GetRandomFileName(), ChunkStore.DirectoryName);

    private readonly StoreWorld _world = new();

    public void Dispose()
    {
        string root = Path.GetDirectoryName(_chunkDirectory)!;
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void AChunkSavedOnUnloadComesBackBeforeItReachesTheDisk()
    {
        Directory.CreateDirectory(BlockedChunkPath(0, 0));

        using var store = new ChunkStore(_chunkDirectory);
        store.QueueSave(ChunkWith(0, 0, BlockRegistry.Stone));

        Chunk? loaded = store.TryLoad(_world, 0, 0);

        Assert.NotNull(loaded);
        Assert.Equal(BlockRegistry.Stone, loaded.GetBlockAt(3, 70, 5).GetBlock());

        Directory.Delete(BlockedChunkPath(0, 0));
    }

    [Fact]
    public void AChunkThatCouldNotBeWrittenIsKeptRatherThanLost()
    {
        Directory.CreateDirectory(BlockedChunkPath(1, 2));

        using (var store = new ChunkStore(_chunkDirectory))
        {
            store.QueueSave(ChunkWith(1, 2, BlockRegistry.Glass));
            store.Flush();

            Chunk? loaded = store.TryLoad(_world, 1, 2);

            Assert.NotNull(loaded);
            Assert.Equal(BlockRegistry.Glass, loaded.GetBlockAt(3, 70, 5).GetBlock());

            Directory.Delete(BlockedChunkPath(1, 2));
        }

        using var reopened = new ChunkStore(_chunkDirectory);
        Chunk? fromDisk = reopened.TryLoad(_world, 1, 2);

        Assert.NotNull(fromDisk);
        Assert.Equal(BlockRegistry.Glass, fromDisk.GetBlockAt(3, 70, 5).GetBlock());
    }

    [Fact]
    public void TheLatestSaveOfAChunkIsTheOneThatLasts()
    {
        using (var store = new ChunkStore(_chunkDirectory))
        {
            store.QueueSave(ChunkWith(-4, 9, BlockRegistry.Stone));
            store.QueueSave(ChunkWith(-4, 9, BlockRegistry.Sand));

            Assert.Equal(BlockRegistry.Sand, store.TryLoad(_world, -4, 9)!.GetBlockAt(3, 70, 5).GetBlock());
        }

        using var reopened = new ChunkStore(_chunkDirectory);

        Assert.Equal(BlockRegistry.Sand, reopened.TryLoad(_world, -4, 9)!.GetBlockAt(3, 70, 5).GetBlock());
    }

    private string BlockedChunkPath(int gridX, int gridZ)
    {
        return Path.Combine(_chunkDirectory, "c." + gridX + "." + gridZ + ".gz");
    }

    private static Chunk ChunkWith(int gridX, int gridZ, Block block)
    {
        var chunk = new Chunk(gridX, gridZ);
        chunk.AddBlockAt(3, 70, 5, BlockRegistry.GetState(block));
        return chunk;
    }

    private sealed class StoreWorld() : World(null!);
}

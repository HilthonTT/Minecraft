using Minecraft.Core.Games;
using Minecraft.Core.Logging;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Storage;

public sealed class WorldStorage : IDisposable
{
    private readonly string _worldDirectory;
    private readonly ChunkStore _chunks;

    public string Directory => _worldDirectory;

    public WorldStorage(string savesRoot, string worldName)
    {
        _worldDirectory = WorldSaves.GetWorldDirectory(savesRoot, worldName);
        _chunks = new ChunkStore(Path.Combine(_worldDirectory, ChunkStore.DirectoryName));
    }

    public void DeleteExistingWorld()
    {
        if (!System.IO.Directory.Exists(_worldDirectory))
        {
            return;
        }

        try
        {
            System.IO.Directory.Delete(_worldDirectory, recursive: true);
            Logger.Info("Discarded the existing world at " + _worldDirectory + ".");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Warn("Could not discard the existing world at " + _worldDirectory + ": " + e.Message);
        }
    }

    public WorldMetadata LoadOrCreateMetadata(int? seed, GameMode? gameMode)
    {
        return WorldMetadataFile.LoadOrCreate(_worldDirectory, seed, gameMode);
    }

    public void SaveMetadata(WorldMetadata metadata) => WorldMetadataFile.Save(_worldDirectory, metadata);

    public Chunk? TryLoadChunk(World world, int gridX, int gridZ) => _chunks.TryLoad(world, gridX, gridZ);

    public void QueueChunkSave(Chunk chunk) => _chunks.QueueSave(chunk);

    public void Flush() => _chunks.Flush();

    public void Dispose() => _chunks.Dispose();
}

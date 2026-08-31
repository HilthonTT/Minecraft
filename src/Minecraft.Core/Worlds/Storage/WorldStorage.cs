using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using Minecraft.Core.Games;
using Minecraft.Core.IO;
using Minecraft.Core.Logging;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Storage;

/// <summary>
/// Reads and writes a world on disk.
/// <para>
/// A save is a directory holding a text <c>level.dat</c> and one gzipped file per stored chunk. Only chunks
/// that were actually modified are stored; everything else is regenerated from the seed, so a world that has
/// merely been walked across costs nothing to save.
/// </para>
/// <para>
/// Chunks are serialised on the calling thread and compressed and written on a background one. Serialising
/// eagerly matters: an unloaded chunk goes straight back into the chunk pool, so deferring it would risk
/// writing a chunk that had already been recycled for another position.
/// </para>
/// </summary>
public sealed class WorldStorage : IDisposable
{
    private const string MetadataFileName = "level.dat";
    private const string ChunkDirectoryName = "chunks";
    private const string ChunkFileExtension = ".gz";

    private readonly string _worldDirectory;
    private readonly string _chunkDirectory;

    private readonly BlockingCollection<PendingChunkSave> _pendingSaves = [];
    private readonly Thread _writerThread;

    /// <summary>Saves accepted but not yet on disk. Lets <see cref="Flush"/> wait for a quiet moment.</summary>
    private int _outstandingSaves;
    private readonly ManualResetEventSlim _drained = new(true);

    private bool _isDisposed;

    /// <summary>The directory this world is stored in. Created on demand by the first write.</summary>
    public string Directory => _worldDirectory;

    private readonly record struct PendingChunkSave(int GridX, int GridZ, byte[] Payload);

    public WorldStorage(string savesRoot, string worldName)
    {
        _worldDirectory = Path.Combine(savesRoot, SanitiseWorldName(worldName));
        _chunkDirectory = Path.Combine(_worldDirectory, ChunkDirectoryName);

        _writerThread = new Thread(RunWriter)
        {
            IsBackground = true,
            Name = "World storage writer",
        };
        _writerThread.Start();
    }

    /// <summary>The directory a world of the given name is stored in, whether or not it exists yet.</summary>
    public static string GetWorldDirectory(string savesRoot, string worldName)
    {
        return Path.Combine(savesRoot, SanitiseWorldName(worldName));
    }

    /// <summary>
    /// Whether a world of that name has been saved before. Asked by the menu, so it can say whether playing
    /// would carry on somebody's game or start a new one, since a seed only ever decides the latter.
    /// </summary>
    public static bool WorldExists(string savesRoot, string worldName)
    {
        return File.Exists(Path.Combine(GetWorldDirectory(savesRoot, worldName), MetadataFileName));
    }

    /// <summary>
    /// The preferred name if nothing is saved under it, or the first numbered variation that is free. Offers
    /// a name that will really create a new world, rather than quietly reopening the last one and leaving a
    /// chosen seed with nothing to decide.
    /// </summary>
    public static string SuggestUnusedWorldName(string savesRoot, string preferred)
    {
        if (!WorldExists(savesRoot, preferred))
        {
            return preferred;
        }

        // Bounded, since a machine with a thousand worlds on it is past the point where guessing helps.
        for (int suffix = 2; suffix < 1000; suffix++)
        {
            string candidate = preferred + suffix;
            if (!WorldExists(savesRoot, candidate))
            {
                return candidate;
            }
        }

        return preferred;
    }

    /// <summary>
    /// Every world saved under the given directory, the most recently played first. A directory without
    /// metadata in it is not a world, so anything else that ended up in there is passed over.
    /// </summary>
    public static List<string> ListWorlds(string savesRoot)
    {
        if (!System.IO.Directory.Exists(savesRoot))
        {
            return [];
        }

        try
        {
            return System.IO.Directory.EnumerateDirectories(savesRoot)
                .Where(directory => File.Exists(Path.Combine(directory, MetadataFileName)))
                .OrderByDescending(System.IO.Directory.GetLastWriteTimeUtc)
                .Select(Path.GetFileName)
                .OfType<string>()
                .ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A saves directory that cannot be read leaves nothing to offer, which the menu shows as having
            // no worlds rather than as a failure to start.
            Logger.Error("Could not read the saves directory at " + savesRoot + ": " + e.Message);
            return [];
        }
    }

    /// <summary>
    /// Renames a saved world. Only ever moves one directory within the saves directory, both ends of which
    /// <see cref="SanitiseWorldName"/> has confined there. Call with no world loaded.
    /// </summary>
    public static WorldRenameResult TryRenameWorld(string savesRoot, string fromName, string toName)
    {
        string source = GetWorldDirectory(savesRoot, fromName);
        string destination = GetWorldDirectory(savesRoot, toName);

        if (!System.IO.Directory.Exists(source))
        {
            return WorldRenameResult.SourceMissing;
        }

        if (source == destination)
        {
            return WorldRenameResult.Unchanged;
        }

        bool differsOnlyByCase = string.Equals(source, destination, StringComparison.OrdinalIgnoreCase);
        if (!differsOnlyByCase && System.IO.Directory.Exists(destination))
        {
            return WorldRenameResult.NameTaken;
        }

        try
        {
            if (differsOnlyByCase)
            {
                // A file system that does not tell capitalisation apart considers the move a no-op and
                // refuses it, so the world is stepped through a name that is unambiguously different.
                string staging = destination + ".renaming";
                System.IO.Directory.Move(source, staging);
                System.IO.Directory.Move(staging, destination);
            }
            else
            {
                System.IO.Directory.Move(source, destination);
            }

            Logger.Info("Renamed world '" + Path.GetFileName(source) + "' to '" + Path.GetFileName(destination) + "'.");
            return WorldRenameResult.Renamed;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Error("Could not rename the world at " + source + ": " + e.Message);
            return WorldRenameResult.Failed;
        }
    }

    /// <summary>
    /// Deletes a saved world for good. Only ever touches one directory inside the saves directory, which
    /// <see cref="SanitiseWorldName"/> has already confined it to. Call with no world loaded.
    /// </summary>
    public static bool TryDeleteWorld(string savesRoot, string worldName)
    {
        string directory = GetWorldDirectory(savesRoot, worldName);
        if (!System.IO.Directory.Exists(directory))
        {
            return false;
        }

        try
        {
            System.IO.Directory.Delete(directory, recursive: true);
            Logger.Info("Deleted the world at " + directory + ".");
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Error("Could not delete the world at " + directory + ": " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Strips anything that cannot appear in a directory name, so a world name coming from a start argument
    /// or typed into the menu cannot escape the saves directory or produce an unopenable path.
    /// </summary>
    public static string SanitiseWorldName(string worldName)
    {
        string trimmed = worldName.Trim();
        if (trimmed.Length == 0)
        {
            return "world";
        }

        char[] invalid = Path.GetInvalidFileNameChars();
        var builder = new System.Text.StringBuilder(trimmed.Length);
        foreach (char character in trimmed)
        {
            builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
        }

        string sanitised = builder.ToString().Trim('.', ' ');
        return sanitised.Length == 0 ? "world" : sanitised;
    }

    /// <summary>
    /// Deletes this world from disk, so the next load generates it afresh from a new seed. Only ever touches
    /// the one directory this instance owns, which <see cref="SanitiseWorldName"/> has already confined to
    /// the saves directory. Call before anything has been read or written.
    /// </summary>
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
            // Better to play on the old world than to refuse to start over a scratch directory.
            Logger.Warn("Could not discard the existing world at " + _worldDirectory + ": " + e.Message);
        }
    }

    /// <summary>
    /// Reads the metadata of an existing world, or describes a new one seeded from <paramref name="seed"/>
    /// when there is nothing on disk yet. Nothing is written until <see cref="SaveMetadata"/> is called.
    /// </summary>
    /// <param name="gameMode">
    /// Which mode to create the world in. Ignored, with a warning, for a world that already exists — the same
    /// rule the seed follows, and for a weaker but real version of the same reason: a world built up in
    /// creative and reopened in survival is a world whose contents were never earned.
    /// </param>
    public WorldMetadata LoadOrCreateMetadata(int? seed, GameMode? gameMode)
    {
        string path = Path.Combine(_worldDirectory, MetadataFileName);

        if (!File.Exists(path))
        {
            int newSeed = seed ?? Random.Shared.Next();
            GameMode newGameMode = gameMode ?? GameMode.Survival;
            Logger.Info(
                "Creating world '" + Path.GetFileName(_worldDirectory) + "' with seed " + newSeed +
                " in " + newGameMode.ToString().ToLowerInvariant() + " mode.");
            return new WorldMetadata { Seed = newSeed, GameMode = newGameMode };
        }

        try
        {
            Dictionary<string, string> fields = ReadKeyValueFile(path);

            int version = ReadInt(fields, "version", WorldMetadata.CurrentVersion);
            if (version != WorldMetadata.CurrentVersion)
            {
                throw new InvalidDataException(
                    $"Save format version {version} cannot be read by this build, which expects " +
                    $"{WorldMetadata.CurrentVersion}.");
            }

            var metadata = new WorldMetadata
            {
                Version = version,
                Seed = ReadInt(fields, "seed", 0),
                CurrentTime = ReadFloat(fields, "time", World.MiddayTimeSeconds),

                // Creative when the key is absent, which is every world saved before there was a choice.
                GameMode = ReadGameMode(fields, "gamemode", GameMode.Creative),
            };

            // An explicit seed cannot be honoured for a world that already exists; its terrain is fixed.
            if (seed.HasValue && seed.Value != metadata.Seed)
            {
                Logger.Warn(
                    "Ignoring seed " + seed.Value + ": world '" + Path.GetFileName(_worldDirectory) +
                    "' already exists with seed " + metadata.Seed + ".");
            }

            if (gameMode.HasValue && gameMode.Value != metadata.GameMode)
            {
                Logger.Warn(
                    "Ignoring game mode " + gameMode.Value + ": world '" + Path.GetFileName(_worldDirectory) +
                    "' already exists in " + metadata.GameMode + " mode.");
            }

            Logger.Info("Loaded world '" + Path.GetFileName(_worldDirectory) + "' with seed " + metadata.Seed + ".");
            return metadata;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or FormatException)
        {
            // Refusing to start beats silently generating a different world over the top of the old one.
            throw new InvalidDataException("Failed to read " + path + ": " + e.Message, e);
        }
    }

    public void SaveMetadata(WorldMetadata metadata)
    {
        try
        {
            System.IO.Directory.CreateDirectory(_worldDirectory);

            string path = Path.Combine(_worldDirectory, MetadataFileName);
            string[] lines =
            [
                "version=" + metadata.Version.ToString(CultureInfo.InvariantCulture),
                "seed=" + metadata.Seed.ToString(CultureInfo.InvariantCulture),
                "time=" + metadata.CurrentTime.ToString("0.###", CultureInfo.InvariantCulture),
                "gamemode=" + metadata.GameMode.ToString().ToLowerInvariant(),
            ];

            WriteFileAtomically(path, stream =>
            {
                using var writer = new StreamWriter(stream);
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            });
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Error("Failed to save world metadata: " + e.Message);
        }
    }

    /// <summary>
    /// Loads the stored chunk at the given position, or returns null when that chunk was never modified and
    /// should be regenerated instead.
    /// </summary>
    public Chunk? TryLoadChunk(World world, int gridX, int gridZ)
    {
        string path = GetChunkPath(gridX, gridZ);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] payload;
            using (FileStream file = File.OpenRead(path))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            using (var buffer = new MemoryStream())
            {
                gzip.CopyTo(buffer);
                payload = buffer.ToArray();
            }

            int head = 0;

            // The payload opens with the same length prefix the network format uses. It is not needed to
            // read the rest, but it is a cheap check that the file is not truncated.
            int declaredSize = DataConverter.BytesToInt32(payload, ref head);
            if (declaredSize != payload.Length - sizeof(int))
            {
                throw new InvalidDataException(
                    $"Expected {declaredSize} bytes of chunk data but the file holds {payload.Length - sizeof(int)}.");
            }

            Chunk chunk = DataConverter.BytesToChunk(payload, world, ref head);

            if (chunk.GridX != gridX || chunk.GridZ != gridZ)
            {
                throw new InvalidDataException(
                    $"Chunk file for ({gridX}, {gridZ}) holds chunk ({chunk.GridX}, {chunk.GridZ}).");
            }

            chunk.MarkClean();
            Logger.Info("Loaded chunk (" + gridX + ", " + gridZ + ") from disk.");
            return chunk;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            // A corrupt chunk falls back to regeneration rather than taking the whole world down.
            Logger.Error("Failed to load chunk (" + gridX + ", " + gridZ + "): " + e.Message + ". Regenerating it.");
            return null;
        }
    }

    /// <summary>
    /// Serialises the chunk now and hands it to the writer thread. Chunks that match what the generator
    /// would produce are skipped, since they can be rebuilt from the seed.
    /// </summary>
    public void QueueChunkSave(Chunk chunk)
    {
        if (_isDisposed || !chunk.IsDirty)
        {
            return;
        }

        byte[] payload;
        using (var buffer = new MemoryStream())
        {
            using (var bufferedStream = new BufferedStream(buffer))
            {
                var writer = new BufferedDataStream(bufferedStream);
                writer.WriteChunk(chunk);
                bufferedStream.Flush();
            }

            payload = buffer.ToArray();
        }

        chunk.MarkClean();

        Interlocked.Increment(ref _outstandingSaves);
        _drained.Reset();

        try
        {
            _pendingSaves.Add(new PendingChunkSave(chunk.GridX, chunk.GridZ, payload));
        }
        catch (InvalidOperationException)
        {
            // The queue was closed by a concurrent shutdown.
            if (Interlocked.Decrement(ref _outstandingSaves) == 0)
            {
                _drained.Set();
            }
        }
    }

    private void RunWriter()
    {
        foreach (PendingChunkSave save in _pendingSaves.GetConsumingEnumerable())
        {
            try
            {
                System.IO.Directory.CreateDirectory(_chunkDirectory);

                WriteFileAtomically(GetChunkPath(save.GridX, save.GridZ), stream =>
                {
                    using var gzip = new GZipStream(stream, CompressionLevel.Optimal);
                    gzip.Write(save.Payload, 0, save.Payload.Length);
                });
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Logger.Error("Failed to save chunk (" + save.GridX + ", " + save.GridZ + "): " + e.Message);
            }
            finally
            {
                if (Interlocked.Decrement(ref _outstandingSaves) == 0)
                {
                    _drained.Set();
                }
            }
        }
    }

    /// <summary>Blocks until every queued chunk has reached disk.</summary>
    public void Flush()
    {
        _drained.Wait();
    }

    /// <summary>
    /// Writes through a temporary file and then moves it into place, so that a crash midway leaves the
    /// previous version intact rather than a half written one.
    /// </summary>
    private static void WriteFileAtomically(string path, Action<Stream> write)
    {
        string temporaryPath = path + ".tmp";

        using (FileStream file = File.Create(temporaryPath))
        {
            write(file);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }

    private string GetChunkPath(int gridX, int gridZ)
    {
        return Path.Combine(
            _chunkDirectory,
            "c." + gridX.ToString(CultureInfo.InvariantCulture) +
            "." + gridZ.ToString(CultureInfo.InvariantCulture) + ChunkFileExtension);
    }

    private static Dictionary<string, string> ReadKeyValueFile(string path)
    {
        Dictionary<string, string> fields = [];

        foreach (string line in File.ReadAllLines(path))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            string[] keyValue = trimmed.Split('=', 2);
            if (keyValue.Length == 2)
            {
                fields[keyValue[0].Trim().ToLowerInvariant()] = keyValue[1].Trim();
            }
        }

        return fields;
    }

    private static int ReadInt(Dictionary<string, string> fields, string key, int fallback)
    {
        if (!fields.TryGetValue(key, out string? value))
        {
            return fallback;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : throw new FormatException($"'{key}' is not a whole number: '{value}'.");
    }

    /// <summary>
    /// Reads a game mode by name. Written as a name rather than a number so that <c>level.dat</c> stays
    /// something a person can read and change by hand, which is the whole reason it is plain text.
    /// </summary>
    private static GameMode ReadGameMode(Dictionary<string, string> fields, string key, GameMode fallback)
    {
        if (!fields.TryGetValue(key, out string? value))
        {
            return fallback;
        }

        // IsDefined as well as TryParse, which on its own accepts any number at all and would hand back a
        // mode the game has never heard of from a level.dat reading 'gamemode=7'.
        return Enum.TryParse(value, ignoreCase: true, out GameMode parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new FormatException($"'{key}' is not a game mode: '{value}'.");
    }

    private static float ReadFloat(Dictionary<string, string> fields, string key, float fallback)
    {
        if (!fields.TryGetValue(key, out string? value))
        {
            return fallback;
        }

        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : throw new FormatException($"'{key}' is not a number: '{value}'.");
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _pendingSaves.CompleteAdding();
        Flush();

        // The writer thread is a background thread, so a hung write cannot keep the process alive.
        _writerThread.Join(TimeSpan.FromSeconds(5));

        _pendingSaves.Dispose();
        _drained.Dispose();
    }
}

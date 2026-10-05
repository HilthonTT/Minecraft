using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using Minecraft.Core.IO;
using Minecraft.Core.Logging;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Storage;

public sealed class ChunkStore : IDisposable
{
    public const string DirectoryName = "chunks";

    private const string FileExtension = ".gz";

    private const int WriterShutdownSeconds = 5;

    private const int MaxWriteAttempts = 5;

    private const int WriteRetryDelayMilliseconds = 50;

    private readonly record struct PendingChunkSave(int GridX, int GridZ, byte[] Payload);

    private readonly string _chunkDirectory;

    private readonly BlockingCollection<PendingChunkSave> _pendingSaves = [];
    private readonly Thread _writerThread;

    private readonly Lock _unwrittenLock = new();
    private readonly Dictionary<(int GridX, int GridZ), byte[]> _unwrittenPayloads = [];

    private int _outstandingSaves;
    private readonly ManualResetEventSlim _drained = new(true);

    private bool _isDisposed;

    public ChunkStore(string chunkDirectory)
    {
        _chunkDirectory = chunkDirectory;

        _writerThread = new Thread(RunWriter)
        {
            IsBackground = true,
            Name = "World storage writer",
        };
        _writerThread.Start();
    }

    public Chunk? TryLoad(World world, int gridX, int gridZ)
    {
        byte[]? unwritten;
        lock (_unwrittenLock)
        {
            _unwrittenPayloads.TryGetValue((gridX, gridZ), out unwritten);
        }

        string path = GetChunkPath(gridX, gridZ);
        if (unwritten is null && !File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] payload = unwritten ?? ReadPayload(path);

            int head = 0;

            int declaredSize = DataConverter.BytesToInt32(payload, ref head);
            if (declaredSize != payload.Length - sizeof(int))
            {
                throw new InvalidDataException(
                    $"Expected {declaredSize} bytes of chunk data but the file holds {payload.Length - sizeof(int)}.");
            }

            Chunk chunk = DataConverter.BytesToChunk(payload, world, ref head);

            if (chunk.GridX != gridX || chunk.GridZ != gridZ)
            {
                world.ChunkPool.ReturnObject(chunk);
                throw new InvalidDataException(
                    $"Chunk file for ({gridX}, {gridZ}) holds chunk ({chunk.GridX}, {chunk.GridZ}).");
            }

            chunk.MarkClean();
            Logger.Info("Loaded chunk (" + gridX + ", " + gridZ + ") from " + (unwritten is null ? "disk." : "its unwritten save."));
            return chunk;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            Logger.Error("Failed to load chunk (" + gridX + ", " + gridZ + "): " + e.Message + ". Regenerating it.");
            return null;
        }
    }

    private static byte[] ReadPayload(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var buffer = new MemoryStream();

        gzip.CopyTo(buffer);
        return buffer.ToArray();
    }

    public void QueueSave(Chunk chunk)
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

        lock (_unwrittenLock)
        {
            _unwrittenPayloads[(chunk.GridX, chunk.GridZ)] = payload;
        }

        Interlocked.Increment(ref _outstandingSaves);
        _drained.Reset();

        try
        {
            _pendingSaves.Add(new PendingChunkSave(chunk.GridX, chunk.GridZ, payload));
        }
        catch (InvalidOperationException)
        {
            MarkSaveFinished();
        }
    }

    public void Flush() => _drained.Wait();

    private void RunWriter()
    {
        foreach (PendingChunkSave save in _pendingSaves.GetConsumingEnumerable())
        {
            try
            {
                WriteUntilSaved(save);
            }
            finally
            {
                MarkSaveFinished();
            }
        }
    }

    private void WriteUntilSaved(PendingChunkSave save)
    {
        for (int attempt = 1; IsLatestSaveOf(save); attempt++)
        {
            try
            {
                Write(save);
                return;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                if (attempt >= MaxWriteAttempts)
                {
                    Logger.Error(
                        "Failed to save chunk (" + save.GridX + ", " + save.GridZ + "): " + e.Message +
                        " Keeping it in memory to try again on shutdown.");
                    return;
                }

                Thread.Sleep(WriteRetryDelayMilliseconds * attempt);
            }
        }
    }

    private void Write(PendingChunkSave save)
    {
        Directory.CreateDirectory(_chunkDirectory);

        AtomicFile.Write(GetChunkPath(save.GridX, save.GridZ), stream =>
        {
            using var gzip = new GZipStream(stream, CompressionLevel.Optimal);
            gzip.Write(save.Payload, 0, save.Payload.Length);
        });

        lock (_unwrittenLock)
        {
            if (IsLatestSaveOf(save))
            {
                _unwrittenPayloads.Remove((save.GridX, save.GridZ));
            }
        }
    }

    private bool IsLatestSaveOf(PendingChunkSave save)
    {
        lock (_unwrittenLock)
        {
            return _unwrittenPayloads.TryGetValue((save.GridX, save.GridZ), out byte[]? latest) &&
                   ReferenceEquals(latest, save.Payload);
        }
    }

    private void WriteLeftovers()
    {
        List<PendingChunkSave> leftovers;
        lock (_unwrittenLock)
        {
            leftovers = _unwrittenPayloads
                .Select(unwritten => new PendingChunkSave(unwritten.Key.GridX, unwritten.Key.GridZ, unwritten.Value))
                .ToList();
        }

        foreach (PendingChunkSave save in leftovers)
        {
            WriteUntilSaved(save);
        }
    }

    private void MarkSaveFinished()
    {
        if (Interlocked.Decrement(ref _outstandingSaves) == 0)
        {
            _drained.Set();
        }
    }

    private string GetChunkPath(int gridX, int gridZ)
    {
        return Path.Combine(
            _chunkDirectory,
            "c." + gridX.ToString(CultureInfo.InvariantCulture) +
            "." + gridZ.ToString(CultureInfo.InvariantCulture) + FileExtension);
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

        _writerThread.Join(TimeSpan.FromSeconds(WriterShutdownSeconds));

        WriteLeftovers();

        _pendingSaves.Dispose();
        _drained.Dispose();
    }
}

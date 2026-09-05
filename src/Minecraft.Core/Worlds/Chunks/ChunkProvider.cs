using Minecraft.Core.Entities.Player;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds.Generation;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Chunks;

public sealed class ChunkProvider
{
    private readonly struct GenerateChunkRequestOutgoing(Vector2 gridPosition, World world)
    {
        public Vector2 GridPosition { get; } = gridPosition;

        public World World { get; } = world;
    }

    private const int MaxVelocitySquaredForLoading = 7225;

    private readonly ServerSession _session;

    private readonly HashSet<Vector2> _currentlyLoadedChunks = [];

    private readonly Queue<GenerateChunkOutput> _receivedChunkData = new();

    private readonly HashSet<GenerateChunkRequestOutgoing> _outgoingChunkRequests = [];

    private readonly Lock _chunkRetrievalLock = new();

    private Queue<(int DistanceToPlayer, Vector2 GridPosition)> _remainingChunkRequests = new();

    private Player? _player;

    public ChunkProvider(ServerSession session)
    {
        _session = session;
        session.OnPlayerAssignedHandler += OnPlayerAssigned;
    }

    private void OnPlayerAssigned()
    {
        _player = _session.Player;
        if (_player is not null)
        {
            _player.OnChunkChangedHandler += OnPlayerChunkChanged;
        }
    }

    private void OnPlayerChunkChanged(World world, Vector2 playerGridPos)
    {
        _remainingChunkRequests = GetChunkLoadQueue(world, playerGridPos);

        UnloadChunks(world, _currentlyLoadedChunks.Where(chunk => !_session.IsChunkVisible(chunk)).ToList());
    }

    private void UnloadChunks(World world, List<Vector2> chunkPositions)
    {
        if (chunkPositions.Count == 0)
        {
            return;
        }

        foreach (Vector2 chunkPos in chunkPositions)
        {
            if (world.LoadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
            {
                world.RemovePlayerPresenceOfChunk(chunk);
            }
            else
            {
                Logger.Warn("Asked to unload chunk " + chunkPos + " that was not loaded on the server.");
            }

            _currentlyLoadedChunks.Remove(chunkPos);
        }

        _session.WritePacket(new ChunkUnloadPacket(chunkPositions));
    }

    private void LoadChunk(World world, Vector2 chunkPos)
    {
        if (world.LoadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            AddPresenceToChunkInWorld(world, chunk);
            return;
        }

        var request = new GenerateChunkRequestOutgoing(chunkPos, world);

        bool requested;
        lock (_chunkRetrievalLock)
        {
            requested = _outgoingChunkRequests.Add(request);
        }

        if (requested)
        {
            ((WorldServer)world).RequestGenerationOfChunk(_player?.ID ?? 0, chunkPos, ChunkRetrievedCallback);
        }
    }

    private void AddPresenceToChunkInWorld(World world, Chunk chunk)
    {
        world.AddPlayerPresenceToChunk(chunk);

        if (_currentlyLoadedChunks.Add(new Vector2(chunk.GridX, chunk.GridZ)))
        {
            _session.WritePacket(new ChunkDataPacket(chunk));
        }
    }

    public void ReleaseAll(World world)
    {
        int unloaded = 0;

        foreach (Vector2 chunkPos in _currentlyLoadedChunks)
        {
            if (world.LoadedChunks.TryGetValue(chunkPos, out Chunk? chunk) && !world.RemovePlayerPresenceOfChunk(chunk))
            {
                continue;
            }

            if (!world.LoadedChunks.ContainsKey(chunkPos))
            {
                unloaded++;
            }
        }

        Logger.Info(
            "Player " + _player?.ID + " let go of " + _currentlyLoadedChunks.Count +
            " chunks, unloading " + unloaded + " of them.");

        _currentlyLoadedChunks.Clear();
        _remainingChunkRequests.Clear();

        lock (_chunkRetrievalLock)
        {
            while (_receivedChunkData.Count > 0)
            {
                _receivedChunkData.Dequeue().Discard();
            }
        }
    }

    private Queue<(int DistanceToPlayer, Vector2 GridPosition)> GetChunkLoadQueue(World world, Vector2 playerGridPosition)
    {
        Queue<(int, Vector2)> visibleChunks = new();

        int dist = _session.PlayerSettings.ViewDistance * 2 + 1;
        float halfDist = dist / 2.0F;

        int x = 0;
        int z = 0;
        int dx = 0;
        int dz = -1;

        for (int i = 0; i < dist * dist; i++)
        {
            if (-halfDist < x && x <= halfDist && -halfDist < z && z <= halfDist)
            {
                Vector2 chunkPos = playerGridPosition + new Vector2(x, z);

                if (!_currentlyLoadedChunks.Contains(chunkPos))
                {
                    var request = new GenerateChunkRequestOutgoing(chunkPos, world);

                    bool shouldEnqueue;
                    lock (_chunkRetrievalLock)
                    {
                        shouldEnqueue = !_outgoingChunkRequests.Contains(request) &&
                                        !_receivedChunkData.Any(data =>
                                            new Vector2(data.Chunk.GridX, data.Chunk.GridZ) == chunkPos);
                    }

                    if (shouldEnqueue)
                    {
                        int maxChunkDistToPlayer = Math.Max(Math.Abs(x), Math.Abs(z));
                        visibleChunks.Enqueue((maxChunkDistToPlayer, chunkPos));
                    }
                }
            }

            if (x == z || (x < 0 && x == -z) || (x > 0 && x == 1 - z))
            {
                (dx, dz) = (-dz, dx);
            }

            x += dx;
            z += dz;
        }

        return visibleChunks;
    }

    private void ChunkRetrievedCallback(GenerateChunkOutput output)
    {
        lock (_chunkRetrievalLock)
        {
            if (_session.State == SessionState.Closed)
            {
                output.Discard();
            }
            else
            {
                _receivedChunkData.Enqueue(output);
            }

            var request = new GenerateChunkRequestOutgoing(
                new Vector2(output.Chunk.GridX, output.Chunk.GridZ),
                output.World);

            if (!_outgoingChunkRequests.Remove(request))
            {
                Logger.Warn("Received a chunk that was not outstanding at " + request.GridPosition);
            }
        }
    }

    public void Update()
    {
        HandleReceivedChunk();

        if (_player is null || _remainingChunkRequests.Count == 0)
        {
            return;
        }

        Vector3 velocity = _player.Velocity;
        int velocitySquared = (int)(velocity.X * velocity.X + velocity.Z * velocity.Z);

        int chunkDist = 0;
        if (velocitySquared < MaxVelocitySquaredForLoading)
        {
            chunkDist = (int)MathUtils.ConvertRange(
                0,
                MaxVelocitySquaredForLoading,
                _session.PlayerSettings.ViewDistance,
                0,
                velocitySquared);
        }

        if (_remainingChunkRequests.Peek().DistanceToPlayer <= chunkDist)
        {
            World? playerWorld = _player.World;
            if (playerWorld is not null)
            {
                LoadChunk(playerWorld, _remainingChunkRequests.Dequeue().GridPosition);
            }
        }
    }

    private void HandleReceivedChunk()
    {
        GenerateChunkOutput output;
        lock (_chunkRetrievalLock)
        {
            if (_receivedChunkData.Count == 0)
            {
                return;
            }

            output = _receivedChunkData.Dequeue();
        }

        Chunk chunk = output.Chunk;
        World world = output.World;
        Vector2 chunkPos = output.GridPosition;

        if (!_session.IsChunkVisible(chunkPos))
        {
            Logger.Warn("Wasted chunk generation at chunk " + chunkPos);
            output.Discard();
            return;
        }

        if (world.LoadedChunks.TryGetValue(chunkPos, out Chunk? alreadyLoaded))
        {
            if (ReferenceEquals(alreadyLoaded, chunk))
            {
                output.Adopt();
            }
            else
            {
                output.Discard();
            }

            AddPresenceToChunkInWorld(world, alreadyLoaded);
            return;
        }

        if (chunk.GridX != (int)chunkPos.X || chunk.GridZ != (int)chunkPos.Y || !world.ChunkPool.IsLentOut(chunk))
        {
            Logger.Warn("Chunk " + chunkPos + " was unloaded again before this session could use it. Asking for it once more.");
            output.Discard();
            LoadChunk(world, chunkPos);
            return;
        }

        output.Adopt();
        AddPresenceToChunkInWorld(world, chunk);
    }
}

using Minecraft.Core.Entities.Player;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds.Generation;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Chunks;

/// <summary>
/// Loads and unloads chunks for one player. Every session has its own, which is what lets a chunk stay
/// loaded for as long as at least one player can see it and no longer.
/// </summary>
public sealed class ChunkProvider
{
    private readonly struct GenerateChunkRequestOutgoing(Vector2 gridPosition, World world)
    {
        /// <summary>The chunk grid position a chunk was requested at.</summary>
        public Vector2 GridPosition { get; } = gridPosition;

        /// <summary>The world the chunk was requested for.</summary>
        public World World { get; } = world;
    }

    /// <summary>
    /// The square of the velocity at which chunk loading is deferred entirely. A player moving this fast
    /// would outrun the terrain generator, so loading waits until they slow down.
    /// </summary>
    private const int MaxVelocitySquaredForLoading = 7225;

    private readonly ServerSession _session;

    /// <summary>All chunk positions currently loaded for the player.</summary>
    private readonly HashSet<Vector2> _currentlyLoadedChunks = [];

    /// <summary>Chunk data that has come back from the generator and is ready to be sent to the player.</summary>
    private readonly Queue<GenerateChunkOutput> _receivedChunkData = new();

    /// <summary>Outgoing requests for chunks that were not already loaded, so none is asked for twice.</summary>
    private readonly HashSet<GenerateChunkRequestOutgoing> _outgoingChunkRequests = [];

    private readonly Lock _chunkRetrievalLock = new();

    /// <summary>The chunk positions still to be asked for, nearest to the player first.</summary>
    private Queue<(int DistanceToPlayer, Vector2 GridPosition)> _remainingChunkRequests = new();

    /// <summary>The session's player, available only once the join handshake has assigned one.</summary>
    private Player? _player;

    public ChunkProvider(ServerSession session)
    {
        _session = session;
        session.OnPlayerAssignedHandler += OnPlayerAssigned;
    }

    private void OnPlayerAssigned()
    {
        _player = _session.Player;
        if (_player != null)
        {
            _player.OnChunkChangedHandler += OnPlayerChunkChanged;
        }
    }

    private void OnPlayerChunkChanged(World world, Vector2 playerGridPos)
    {
        _remainingChunkRequests = GetChunkLoadQueue(world, playerGridPos);

        // Unload everything that has fallen outside the view distance.
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

    /// <summary>
    /// Sends the chunk straight to the player if the server already has it, otherwise asks the world
    /// generator to produce it.
    /// </summary>
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

    /// <summary>
    /// Builds the queue of chunk positions the player still needs, walking outwards from the player in a
    /// spiral so that the nearest chunks are always requested first.
    /// </summary>
    private Queue<(int DistanceToPlayer, Vector2 GridPosition)> GetChunkLoadQueue(World world, Vector2 playerGridPosition)
    {
        Queue<(int, Vector2)> visibleChunks = new();

        // The visible area is a square with sides (view distance * 2) + 1 centred on the player.
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
                        // Skip anything already asked for, and anything already generated but not yet handed
                        // over to the player.
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

            // Turn at each corner of the spiral.
            if (x == z || (x < 0 && x == -z) || (x > 0 && x == 1 - z))
            {
                (dx, dz) = (-dz, dx);
            }

            x += dx;
            z += dz;
        }

        return visibleChunks;
    }

    /// <summary>Called from the world generator thread once a requested chunk has been generated.</summary>
    private void ChunkRetrievedCallback(GenerateChunkOutput output)
    {
        lock (_chunkRetrievalLock)
        {
            _receivedChunkData.Enqueue(output);

            var request = new GenerateChunkRequestOutgoing(
                new Vector2(output.Chunk.GridX, output.Chunk.GridZ),
                output.World);

            if (!_outgoingChunkRequests.Remove(request))
            {
                Logger.Warn("Received a chunk that was not outstanding at " + request.GridPosition);
            }
        }
    }

    public void Update(float deltaTimeSeconds)
    {
        HandleReceivedChunk();

        if (_player == null || _remainingChunkRequests.Count == 0)
        {
            return;
        }

        // The faster the player moves, the tighter the radius chunks are loaded within, so that generation
        // effort is not spent on chunks that will be behind them by the time they arrive.
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
            if (playerWorld != null)
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
        if (_session.IsChunkVisible(new Vector2(chunk.GridX, chunk.GridZ)))
        {
            AddPresenceToChunkInWorld(output.World, chunk);
        }
        else
        {
            Logger.Warn("Wasted chunk generation at chunk " + chunk.GridX + ", " + chunk.GridZ);
        }
    }
}

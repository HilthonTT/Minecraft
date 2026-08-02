using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.NetHandler;

/// <summary>
/// Handles the packets a server receives from a single client. Packets a server should never receive throw,
/// since receiving one means the client is confused about which side it is on.
/// </summary>
public sealed class ServerNetHandler : INetHandler
{
    private readonly Game _game;
    private ServerSession _session = null!;

    public ServerNetHandler(Game game)
    {
        _game = game;
    }

    public void AssignSession(ServerSession session) => _session = session;

    public void ProcessPlaceBlockPacket(PlaceBlockPacket blockPacket)
    {
        _game.Server.World.QueueToAddBlockAt(blockPacket.BlockPos, blockPacket.BlockState);
    }

    public void ProcessRemoveBlockPacket(RemoveBlockPacket removeBlockPacket)
    {
        foreach (Vector3i blockPos in removeBlockPacket.BlockPositions)
        {
            _game.Server.World.QueueToRemoveBlockAt(blockPos);
        }
    }

    public void ProcessChatPacket(ChatPacket chatPacket)
    {
        Logger.Info("Server received message " + chatPacket.Message);
        _game.Server.BroadcastPacket(chatPacket);
    }

    public void ProcessEntityDataPacket(EntityDataPacket entityDataPacket)
    {
        // A client may only move the player it was given. Anything else is either one that has not finished
        // joining and is still reporting the position it was built with, or one reaching for another entity.
        if (_session.Player is null || entityDataPacket.EntityID != _session.Player.ID)
        {
            return;
        }

        _session.Player.Position = entityDataPacket.Position;
        _session.Player.Velocity = entityDataPacket.Velocity;
        _session.Player.Yaw = entityDataPacket.Yaw;
        _game.Server.BroadcastPacketExceptTo(_session, entityDataPacket);
    }

    public void ProcessJoinRequestPacket(PlayerJoinRequestPacket playerJoinRequestPacket)
    {
        string playerName = playerJoinRequestPacket.Name.Trim();
        if (playerName.Length == 0 || playerName == "Player")
        {
            _session.WritePacket(new PlayerLeavePacket(0, LeaveReason.Banned, "You are not allowed on this server."));
            _session.State = SessionState.Closed;
            return;
        }

        int playerId = _game.Server.World.GenerateEntityId();
        string serverPlayerName = playerName + "-" + playerId % 10000;
        Vector3 spawnPosition = _game.Server.World.GenerateAndGetValidSpawn();

        var player = new ServerPlayer(playerId, serverPlayerName, _game.Server.World, spawnPosition);
        _session.AssignPlayer(player);

        _game.Server.World.SpawnEntity(player);
        _session.WritePacket(new PlayerJoinAcceptPacket(
            serverPlayerName,
            playerId,
            spawnPosition,
            _game.Server.World.Environment.CurrentTime));
        _session.State = SessionState.Accepted;

        // Let everyone already online know about the new player.
        _game.Server.BroadcastPacketExceptTo(_session, new PlayerJoinPacket(serverPlayerName, playerId));

        // And let the new player know about everyone already online.
        foreach (Session.Session client in _game.Server.ConnectedClients)
        {
            if (client.Player is not null && client.Player != player)
            {
                _session.WritePacket(new PlayerJoinPacket(client.Player.Name, client.Player.ID));
            }
        }
    }

    public void ProcessPlayerBlockInteractionpacket(PlayerBlockInteractionPacket playerInteractionPacket)
    {
        Vector3i blockPos = playerInteractionPacket.BlockPos;
        BlockState state = _game.Server.World.GetBlockAt(blockPos);
        state.GetBlock().OnInteract(state, blockPos, _game.Server.World);

        foreach (ServerSession clientSession in _game.Server.ConnectedClients)
        {
            if (clientSession.IsBlockPositionInViewRange(blockPos))
            {
                clientSession.WritePacket(playerInteractionPacket);
            }
        }
    }

    public void ProcessPlayerKeepAlivePacket(PlayerKeepAlivePacket keepAlivePacket)
    {
        _game.Server.UpdateKeepAliveFor(_session);
    }

    public void ProcessPlayerLeavePacket(PlayerLeavePacket playerKickPacket)
    {
        // A client announcing its own departure is advisory; the session closing is what actually removes it.
        Logger.Info("Client " + _session.Player?.ID + " announced it is leaving: " + playerKickPacket.Message);
        _session.State = SessionState.Closed;
    }

    public void ProcessChunkDataPacket(ChunkDataPacket chunkDataPacket) =>
        throw new InvalidOperationException("A server does not receive chunk data.");

    public void ProcessChunkUnloadPacket(ChunkUnloadPacket unloadChunkPacket) =>
        throw new InvalidOperationException("A server does not receive chunk unloads.");

    public void ProcessPlayerJoinPacket(PlayerJoinPacket playerJoinPacket) =>
        throw new InvalidOperationException("A server does not receive player joins.");

    public void ProcessJoinAcceptPacket(PlayerJoinAcceptPacket playerJoinAcceptPacket) =>
        throw new InvalidOperationException("A server does not receive join accepts.");

    public void ProcessEntitySpawnPacket(EntitySpawnPacket entitySpawnPacket) =>
        throw new InvalidOperationException("A server does not receive entity spawns.");

    public void ProcessEntityDespawnPacket(EntityDespawnPacket entityDespawnPacket) =>
        throw new InvalidOperationException("A server does not receive entity despawns.");
}

using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
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
    /// <summary>
    /// What a bare fist takes off, which is what everyone is swinging: there is nothing to hold yet that
    /// hits harder. The same figure the game this is modelled on gives an empty hand, so the mob healths
    /// borrowed along with it come out at the number of punches they are supposed to.
    /// </summary>
    private const int PunchDamage = 1;

    /// <summary>
    /// How far from a player a mob may be and still be hit. Well beyond the arm's length a client will let
    /// anyone aim at: the position held here for a player is a tenth of a second behind where they actually
    /// are and the mob has moved since as well, so a check this side has to leave room for both.
    /// </summary>
    private const float MaxAttackReach = 6F;

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

    public void ProcessPlayerSettingsPacket(PlayerSettingsPacket playerSettingsPacket)
    {
        _session.SetViewDistance(playerSettingsPacket.ViewDistance);
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

    /// <summary>
    /// A player swinging at a mob. Everything about the blow is decided here — the client is only reporting
    /// what it aimed at — and what comes back out of it is a hurt packet to everyone who can see the mob,
    /// followed by the mob itself if that was the last blow it had in it.
    /// </summary>
    public void ProcessPlayerAttackEntityPacket(PlayerAttackEntityPacket playerAttackEntityPacket)
    {
        if (_session.Player is not ServerPlayer attacker)
        {
            return;
        }

        if (!_game.Server.World.LoadedEntities.TryGetValue(playerAttackEntityPacket.EntityID, out Entity? target) ||
            target is not Mob mob)
        {
            // A mob that died or wandered off between the swing and the packet arriving. Both are ordinary.
            return;
        }

        if ((mob.Position - attacker.Position).LengthSquared > MaxAttackReach * MaxAttackReach)
        {
            Logger.Warn("Player " + attacker.ID + " swung at a mob out of reach.");
            return;
        }

        // False while the mob is still inside the half second the last blow bought it, which is what a
        // client holding the mouse button down runs into.
        if (!mob.TryHurt(PunchDamage, attacker))
        {
            return;
        }

        BroadcastHurt(mob);

        if (!mob.IsAlive)
        {
            _game.Server.World.DespawnEntity(mob.ID);
        }
    }

    /// <summary>
    /// Tells everyone near enough to see the mob that it was hit. Sent to the attacker as well as to the
    /// onlookers: the client that swung applies nothing off its own bat, the same as with a block.
    /// </summary>
    private void BroadcastHurt(Mob mob)
    {
        var packet = new EntityHurtPacket(mob.ID, died: !mob.IsAlive);

        foreach (ServerSession clientSession in _game.Server.ConnectedClients)
        {
            if (clientSession.IsChunkVisible(Worlds.World.GetChunkPosition(mob.Position.X, mob.Position.Z)))
            {
                clientSession.WritePacket(packet);
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

    public void ProcessExplosionPacket(ExplosionPacket explosionPacket) =>
        throw new InvalidOperationException("A server does not receive explosions; it is the one that sets them off.");

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

    public void ProcessEntityHurtPacket(EntityHurtPacket entityHurtPacket) =>
        throw new InvalidOperationException("A server does not receive damage; it is the one that deals it.");
}

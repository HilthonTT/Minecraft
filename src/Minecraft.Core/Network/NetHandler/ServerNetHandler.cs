using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Worlds;
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

    /// <summary>
    /// How far from a player a block may be and still drop something when it is broken. Well beyond the
    /// forty a client will let anyone aim at, since the position held here is a tenth of a second old, but
    /// finite: a request to break a block on the other side of the world is not a request to be paid for one.
    /// </summary>
    private const float MaxDropReach = 64F;

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
        // A break by hand is one block. Everything that arrives here carrying more of them is a debug tool
        // clearing a volume, and none of that should be paid out as a pile of drops on the floor.
        bool isSingleBreak = removeBlockPacket.BlockPositions.Length == 1;

        bool isSurvival = _session.Player is ServerPlayer { IsCreative: false };

        foreach (Vector3i blockPos in removeBlockPacket.BlockPositions)
        {
            if (isSurvival && !MayBreak(blockPos))
            {
                continue;
            }

            if (isSingleBreak && isSurvival)
            {
                DropContentsOf(blockPos);
            }

            _game.Server.World.QueueToRemoveBlockAt(blockPos);
        }
    }

    /// <summary>
    /// Whether a player playing for keeps is allowed through this block at all. Bedrock is the floor of the
    /// world, and a client will not have let anyone dig at it, so one asking has been told otherwise.
    /// </summary>
    private bool MayBreak(Vector3i blockPos)
    {
        if (_game.Server.World.GetBlockAt(blockPos).GetBlock().IsBreakable)
        {
            return true;
        }

        Logger.Warn("Player " + _session.Player?.ID + " asked to break an unbreakable block at " + blockPos + ".");
        return false;
    }

    /// <summary>
    /// Throws out whatever the block being broken leaves behind.
    /// <para>
    /// Done here rather than off the world's own removal, which everything goes through: water washes
    /// flowers away, a bank of sand settles a cell at a time and a blast takes a hillside apart, and every
    /// one of those is an ordinary removal. Only a player swinging at a block earns anything, and this is
    /// the one place that knows a swing is what this was.
    /// </para>
    /// </summary>
    private void DropContentsOf(Vector3i blockPos)
    {
        if (_session.Player is not ServerPlayer breaker)
        {
            return;
        }

        WorldServer world = _game.Server.World;

        BlockState state = world.GetBlockAt(blockPos);
        Block block = state.GetBlock();

        if (block == BlockRegistry.Air)
        {
            return;
        }

        var centre = new Vector3(blockPos.X + 0.5F, blockPos.Y + 0.5F, blockPos.Z + 0.5F);
        if ((centre - breaker.Position).LengthSquared > MaxDropReach * MaxDropReach)
        {
            Logger.Warn("Player " + breaker.ID + " broke a block out of reach at " + blockPos + ".");
            return;
        }

        Block? dropped = block.GetDroppedBlock(state);
        if (dropped is not null)
        {
            // Hung off the removal rather than thrown out now: the block is still standing here until the
            // end of the next world update, and anything put in a solid cell is lifted out onto the top of
            // it. See WorldServer.DropWhenRemoved.
            world.DropWhenRemoved(blockPos, new ItemStack(dropped, 1));
        }
    }

    public void ProcessChatPacket(ChatPacket chatPacket)
    {
        // A line starting with a slash is a request rather than something to say, and is answered to the one
        // player who typed it instead of being repeated to the room.
        if (ChatCommands.TryHandle(_game, _session, chatPacket.Message))
        {
            return;
        }

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

        // Everyone arrives in whatever mode the world is played in. There is no player database to remember
        // anyone by, so the world is the only thing that can answer the question.
        player.SetGameMode(_game.Server.World.DefaultGameMode);

        _session.AssignPlayer(player);

        _game.Server.World.SpawnEntity(player);
        _session.WritePacket(new PlayerJoinAcceptPacket(
            serverPlayerName,
            playerId,
            spawnPosition,
            _game.Server.World.Environment.CurrentTime,
            player.GameMode,
            player.Health));
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

        // Does nothing while the mob is still inside the window a blow of at least this weight already
        // bought it, which is what a client holding the mouse button down runs into. Everything else —
        // telling the onlookers, and taking a killed mob out of the world — belongs to the world.
        _game.Server.World.HurtMob(mob, PunchDamage, attacker.Position, attacker);
    }

    /// <summary>
    /// A player reporting a fall. What it cost is decided here, the same way a punch is: the client has just
    /// simulated the body and is the only thing that can say how far it dropped, and this is the only thing
    /// that can say what that is worth.
    /// </summary>
    public void ProcessPlayerFellPacket(PlayerFellPacket playerFellPacket)
    {
        if (_session.Player is not ServerPlayer player)
        {
            return;
        }

        float fallen = playerFellPacket.FallenBlocks;

        // A fall longer than the world is tall did not happen. Anything the client reports is only ever a
        // request to be hurt, so the worst a bad one can do is ask for nothing.
        if (!float.IsFinite(fallen) || fallen > Constants.MAX_BUILD_HEIGHT)
        {
            return;
        }

        var damage = (int)MathF.Floor(fallen - Constants.PLAYER_SAFE_FALL_BLOCKS);
        if (damage > 0)
        {
            _game.Server.World.HurtPlayer(player, damage);
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

    public void ProcessPlayerGameModePacket(PlayerGameModePacket playerGameModePacket) =>
        throw new InvalidOperationException("A server does not receive game modes; a client asks through the chat.");

    public void ProcessPlayerHealthPacket(PlayerHealthPacket playerHealthPacket) =>
        throw new InvalidOperationException("A server does not receive health; it is the one that keeps it.");

    public void ProcessPlayerRespawnPacket(PlayerRespawnPacket playerRespawnPacket) =>
        throw new InvalidOperationException("A server does not receive respawns; it is the one that orders them.");

    public void ProcessItemSpawnPacket(ItemSpawnPacket itemSpawnPacket) =>
        throw new InvalidOperationException("A server does not receive item spawns.");

    public void ProcessItemPickupPacket(ItemPickupPacket itemPickupPacket) =>
        throw new InvalidOperationException("A server does not receive pickups; it is the one that grants them.");
}

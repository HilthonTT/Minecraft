using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.NetHandler;

public sealed class ServerNetHandler : INetHandler
{
    private const int PunchDamage = 1;

    private const float MaxAttackReach = 6F;

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

    private bool MayBreak(Vector3i blockPos)
    {
        if (_game.Server.World.GetBlockAt(blockPos).GetBlock().IsBreakable)
        {
            return true;
        }

        Logger.Warn("Player " + _session.Player?.ID + " asked to break an unbreakable block at " + blockPos + ".");
        return false;
    }

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

        if (!Harvesting.CanHarvest(block, breaker.HeldItem))
        {
            return;
        }

        ItemStack dropped = block.GetDrop(state);
        if (!dropped.IsEmpty)
        {
            world.DropWhenRemoved(blockPos, dropped);
        }
    }

    public void ProcessChatPacket(ChatPacket chatPacket)
    {
        if (ChatCommands.TryHandle(_game, _session, chatPacket.Message))
        {
            return;
        }

        Logger.Info("Server received message " + chatPacket.Message);
        _game.Server.BroadcastPacket(chatPacket);
    }

    public void ProcessEntityDataPacket(EntityDataPacket entityDataPacket)
    {
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

        _game.Server.BroadcastPacketExceptTo(_session, new PlayerJoinPacket(serverPlayerName, playerId));

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

    public void ProcessPlayerAttackEntityPacket(PlayerAttackEntityPacket playerAttackEntityPacket)
    {
        if (_session.Player is not ServerPlayer attacker)
        {
            return;
        }

        if (!_game.Server.World.LoadedEntities.TryGetValue(playerAttackEntityPacket.EntityID, out Entity? target) ||
            target is not Mob mob)
        {
            return;
        }

        if ((mob.Position - attacker.Position).LengthSquared > MaxAttackReach * MaxAttackReach)
        {
            Logger.Warn("Player " + attacker.ID + " swung at a mob out of reach.");
            return;
        }

        int damage = attacker.HeldItem.Tool?.AttackDamage ?? PunchDamage;

        _game.Server.World.HurtMob(mob, damage, attacker.Position, attacker);
    }

    public void ProcessPlayerFellPacket(PlayerFellPacket playerFellPacket)
    {
        if (_session.Player is not ServerPlayer player)
        {
            return;
        }

        float fallen = playerFellPacket.FallenBlocks;

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

    public void ProcessPlayerDropItemPacket(PlayerDropItemPacket playerDropItemPacket)
    {
        if (_session.Player is not ServerPlayer thrower)
        {
            return;
        }

        if (thrower.IsCreative)
        {
            Logger.Warn("Player " + thrower.ID + " tried to throw an item down in creative.");
            return;
        }

        if (playerDropItemPacket.Count is <= 0 or > ItemStack.MaxCount)
        {
            Logger.Warn("Player " + thrower.ID + " tried to throw " + playerDropItemPacket.Count + " of something.");
            return;
        }

        Item? thrown = ItemRegistry.TryGet(playerDropItemPacket.ItemId);
        if (thrown is null || thrown == ItemRegistry.For(BlockRegistry.Air))
        {
            Logger.Warn("Player " + thrower.ID + " tried to throw item id " + playerDropItemPacket.ItemId + ".");
            return;
        }

        _game.Server.World.ThrowDroppedItem(
            thrower,
            new ItemStack(thrown, playerDropItemPacket.Count, playerDropItemPacket.Damage));
    }

    public void ProcessPlayerHeldItemPacket(PlayerHeldItemPacket playerHeldItemPacket)
    {
        if (_session.Player is not ServerPlayer player)
        {
            return;
        }

        Item? held = ItemRegistry.TryGet(playerHeldItemPacket.ItemId);

        player.HeldItem = held is null
            ? ItemStack.Empty
            : new ItemStack(held, 1, playerHeldItemPacket.Damage);
    }

    public void ProcessPlayerKeepAlivePacket(PlayerKeepAlivePacket keepAlivePacket)
    {
        _game.Server.UpdateKeepAliveFor(_session);
    }

    public void ProcessPlayerLeavePacket(PlayerLeavePacket playerKickPacket)
    {
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

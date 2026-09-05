using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Render.UI.Presets;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.NetHandler;

public sealed class ClientNetHandler : INetHandler
{
    private readonly Game _game;
    private ClientSession _session = null!;

    public ClientNetHandler(Game game)
    {
        _game = game;
    }

    public void AssignSession(ClientSession session) => _session = session;

    public void ProcessPlaceBlockPacket(PlaceBlockPacket placeBlockPacket)
    {
        _game.World.QueueToAddBlockAt(placeBlockPacket.BlockPos, placeBlockPacket.BlockState);
    }

    public void ProcessRemoveBlockPacket(RemoveBlockPacket removeBlockPacket)
    {
        _game.World.QueueToRemoveBlocksAt(removeBlockPacket.BlockPositions);
    }

    public void ProcessChatPacket(ChatPacket chatPacket)
    {
        if (chatPacket.Sender.Length == 0)
        {
            _game.MasterRenderer.IngameCanvas.AddSystemMessage(chatPacket.Message);
            return;
        }

        _game.MasterRenderer.IngameCanvas.AddUserMessage(chatPacket.Sender, chatPacket.Message);
    }

    public void ProcessChunkDataPacket(ChunkDataPacket chunkDataPacket)
    {
        _game.World.AddPlayerPresenceToChunk(chunkDataPacket.Chunk);
    }

    public void ProcessExplosionPacket(ExplosionPacket explosionPacket)
    {
        _game.SoundDirector.OnExplosion(explosionPacket.Position);
        _game.MasterRenderer.Particles.OnExplosion(_game.World, explosionPacket.Position);
    }

    public void ProcessChunkUnloadPacket(ChunkUnloadPacket unloadChunkPacket)
    {
        foreach (Vector2 chunkGridPosition in unloadChunkPacket.ChunkGridPositions)
        {
            if (_game.World.LoadedChunks.TryGetValue(chunkGridPosition, out Chunk? chunk))
            {
                if (!_game.World.RemovePlayerPresenceOfChunk(chunk))
                {
                    Logger.Warn("Server asked to unload chunk " + chunkGridPosition + " that was not loaded.");
                }
            }
        }
    }

    public void ProcessEntityDataPacket(EntityDataPacket entityDataPacket)
    {
        if (!_game.World.LoadedEntities.TryGetValue(entityDataPacket.EntityID, out Entity? entity))
        {
            Logger.Error("Received positional data for unregistered entity " + entityDataPacket.EntityID);
            return;
        }

        if (entity is not OtherClientPlayer and not Mob and not DroppedItem)
        {
            Logger.Error("Received positional data for an entity this side owns: " + entity.GetType());
            return;
        }

        entity.ServerPosition = entityDataPacket.Position;
        entity.ServerYaw = entityDataPacket.Yaw;
    }

    public void ProcessEntitySpawnPacket(EntitySpawnPacket entitySpawnPacket)
    {
        if (_game.World.LoadedEntities.ContainsKey(entitySpawnPacket.EntityID))
        {
            return;
        }

        Mob? mob = MobFactory.Create(
            entitySpawnPacket.EntityType,
            entitySpawnPacket.EntityID,
            _game.World,
            entitySpawnPacket.Position);

        if (mob is null)
        {
            Logger.Error("Server spawned an entity of type " + entitySpawnPacket.EntityType + ", which is not a mob.");
            return;
        }

        mob.ServerPosition = entitySpawnPacket.Position;
        mob.Yaw = entitySpawnPacket.Yaw;
        mob.ServerYaw = entitySpawnPacket.Yaw;

        _game.World.SpawnEntity(mob);
    }

    public void ProcessEntityDespawnPacket(EntityDespawnPacket entityDespawnPacket)
    {
        if (_game.World.LoadedEntities.ContainsKey(entityDespawnPacket.EntityID))
        {
            _game.World.DespawnEntity(entityDespawnPacket.EntityID);
        }
    }

    public void ProcessEntityHurtPacket(EntityHurtPacket entityHurtPacket)
    {
        if (!_game.World.LoadedEntities.TryGetValue(entityHurtPacket.EntityID, out Entity? entity))
        {
            return;
        }

        _game.SoundDirector.OnEntityHurt(entity, entityHurtPacket.Died);

        if (entity is Mob mob)
        {
            mob.ShowHurt();
        }
    }

    public void ProcessPlayerGameModePacket(PlayerGameModePacket playerGameModePacket)
    {
        _game.ClientPlayer.SetGameMode(playerGameModePacket.GameMode);
    }

    public void ProcessPlayerHealthPacket(PlayerHealthPacket playerHealthPacket)
    {
        bool died = playerHealthPacket.Health <= 0 && _game.ClientPlayer.Health > 0;

        _game.ClientPlayer.SetHealth(playerHealthPacket.Health);

        if (playerHealthPacket.WasHurt)
        {
            _game.SoundDirector.OnPlayerHurt(_game.ClientPlayer.Position);
        }

        if (died)
        {
            _game.MasterRenderer.IngameCanvas.AddSystemMessage("You died.");
        }
    }

    public void ProcessPlayerRespawnPacket(PlayerRespawnPacket playerRespawnPacket)
    {
        _game.ClientPlayer.RespawnAt(playerRespawnPacket.SpawnPosition);
    }

    public void ProcessItemSpawnPacket(ItemSpawnPacket itemSpawnPacket)
    {
        if (_game.World.LoadedEntities.ContainsKey(itemSpawnPacket.EntityID))
        {
            return;
        }

        Item? dropped = ItemRegistry.TryGet(itemSpawnPacket.ItemId);
        if (dropped is null)
        {
            Logger.Warn("Server spawned unknown item id " + itemSpawnPacket.ItemId + ".");
            return;
        }

        var stack = new ItemStack(dropped, itemSpawnPacket.Count, itemSpawnPacket.Damage);

        var item = new DroppedItem(
            itemSpawnPacket.EntityID,
            _game.World,
            itemSpawnPacket.Position,
            stack)
        {
            ServerPosition = itemSpawnPacket.Position,
        };

        _game.World.SpawnEntity(item);
    }

    public void ProcessItemPickupPacket(ItemPickupPacket itemPickupPacket)
    {
        if (_game.World.LoadedEntities.ContainsKey(itemPickupPacket.EntityID))
        {
            _game.World.DespawnEntity(itemPickupPacket.EntityID);
        }

        Item? collected = ItemRegistry.TryGet(itemPickupPacket.ItemId);
        if (collected is null)
        {
            Logger.Warn("Server granted unknown item id " + itemPickupPacket.ItemId + ".");
            return;
        }

        var picked = new ItemStack(collected, itemPickupPacket.Count, itemPickupPacket.Damage);

        ItemStack leftover = _game.ClientPlayer.Inventory.TryAdd(picked);
        _game.ClientPlayer.ThrowAway(leftover);

        if (leftover.Count < picked.Count)
        {
            _game.SoundDirector.OnItemPickedUp(_game.ClientPlayer.Position);
        }
    }

    public void ProcessPlayerFellPacket(PlayerFellPacket playerFellPacket)
    {
        throw new InvalidOperationException("A client does not receive falls; it is the one that reports them.");
    }

    public void ProcessPlayerDropItemPacket(PlayerDropItemPacket playerDropItemPacket)
    {
        throw new InvalidOperationException("A client does not receive drops; it is the one that throws them.");
    }

    public void ProcessPlayerHeldItemPacket(PlayerHeldItemPacket playerHeldItemPacket)
    {
        throw new InvalidOperationException("A client does not receive what is held; it is the one holding it.");
    }

    public void ProcessJoinRequestPacket(PlayerJoinRequestPacket playerJoinRequestPacket)
    {
        throw new InvalidOperationException();
    }

    public void ProcessPlayerAttackEntityPacket(PlayerAttackEntityPacket playerAttackEntityPacket)
    {
        throw new InvalidOperationException();
    }

    public void ProcessJoinAcceptPacket(PlayerJoinAcceptPacket playerJoinAcceptPacket)
    {
        Logger.Info("You: " + playerJoinAcceptPacket.Name + " connected.");

        _game.ClientPlayer.ID = playerJoinAcceptPacket.PlayerID;
        _game.ClientPlayer.Name = playerJoinAcceptPacket.Name;
        _game.ClientPlayer.Position = playerJoinAcceptPacket.SpawnPosition;

        _game.ClientPlayer.SetGameMode(playerJoinAcceptPacket.GameMode);
        _game.ClientPlayer.SetHealth(playerJoinAcceptPacket.Health);

        _session.State = SessionState.Accepted;

        _game.ClientPlayer.ReportHeldItem();

        _game.World.Environment.CurrentTime = playerJoinAcceptPacket.CurrentTime;

        _game.World.SpawnEntity(_game.ClientPlayer);

        if (_game.IsServer)
        {
            _game.MasterRenderer.IngameCanvas.AddSystemMessage(
                "World '" + _game.WorldName + "', seed " + _game.Server.World.Seed);

            _game.MasterRenderer.IngameCanvas.AddSystemMessage(
                "Hosting this world. Others can join at " +
                NetworkAddresses.LocalAddress + ":" + _game.Server.Port);
        }
    }

    public void ProcessPlayerJoinPacket(PlayerJoinPacket playerJoinPacket)
    {
        OtherClientPlayer otherPlayer = new(playerJoinPacket.PlayerID, playerJoinPacket.Name, _game.World);
        UICanvasEntityName playerNameCanvas = new(_game, otherPlayer, playerJoinPacket.Name);
        _game.MasterRenderer.AddCanvas(playerNameCanvas);
        _game.World.SpawnEntity(otherPlayer);
        _game.MasterRenderer.IngameCanvas.AddSystemMessage(playerJoinPacket.Name + " joined the game");
    }

    public void ProcessPlayerLeavePacket(PlayerLeavePacket playerLeavePacket)
    {
        if (playerLeavePacket.ID == 0)
        {
            Logger.Info("You were disconnected for reason: " + playerLeavePacket.Reason + " Message: " + playerLeavePacket.Message);
            _session.State = SessionState.Closed;
            return;
        }

        Logger.Info("Player " + playerLeavePacket.ID + " left for reason " + playerLeavePacket.Reason + " with message " + playerLeavePacket.Message);

        if (_game.World.LoadedEntities.TryGetValue(playerLeavePacket.ID, out Entity? leavingEntity) &&
            leavingEntity is OtherClientPlayer leavingPlayer)
        {
            _game.MasterRenderer.IngameCanvas.AddSystemMessage(leavingPlayer.Name + " left the game");
        }

        _game.World.DespawnEntity(playerLeavePacket.ID);
    }

    public void ProcessPlayerBlockInteractionpacket(PlayerBlockInteractionPacket playerInteractionPacket)
    {
        Vector3i blockPos = playerInteractionPacket.BlockPos;
        BlockState state = _game.World.GetBlockAt(blockPos);
        state.GetBlock().OnInteract(state, blockPos, _game.World);
    }

    public void ProcessPlayerKeepAlivePacket(PlayerKeepAlivePacket keepAlivePacket)
    {
        throw new InvalidOperationException();
    }

    public void ProcessPlayerSettingsPacket(PlayerSettingsPacket playerSettingsPacket)
    {
        throw new InvalidOperationException();
    }
}

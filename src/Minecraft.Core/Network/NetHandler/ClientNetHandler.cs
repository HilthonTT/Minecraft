using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;
using Minecraft.Core.Render.UI.Presets;

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

    /// <summary>
    /// Something said, or something the game itself is saying. An empty sender is what marks the second: a
    /// command's answer went to one player and belongs to nobody, so it is drawn in the game's own colour
    /// rather than dressed up as a message from a player with no name.
    /// </summary>
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
        // The tracker on the server only sends a spawn for something it has not already told us about, but a
        // duplicate would otherwise replace a live entity with a fresh one that has to interpolate in again.
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
        // Despawns for entities that were never tracked are not worth complaining about: a mob can leave
        // range in the same update it would first have been sent in.
        if (_game.World.LoadedEntities.ContainsKey(entityDespawnPacket.EntityID))
        {
            _game.World.DespawnEntity(entityDespawnPacket.EntityID);
        }
    }

    /// <summary>
    /// A mob having been hit, which is all this side is told: it plays the mob's own cry and marks it red
    /// for as long as the blow keeps it from being hit again. A death is followed by an ordinary despawn a
    /// moment later, which is what actually takes the mob out of the world.
    /// </summary>
    public void ProcessEntityHurtPacket(EntityHurtPacket entityHurtPacket)
    {
        // Nothing to show and nowhere to play it from. A mob can leave this client's range in the same
        // update the blow that killed it was reported in.
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

    /// <summary>
    /// The mode this player has been put into. What it changes is not only what the controls do: the
    /// inventory is a different thing in each of the two, so it is started over rather than carried across.
    /// </summary>
    public void ProcessPlayerGameModePacket(PlayerGameModePacket playerGameModePacket)
    {
        _game.ClientPlayer.SetGameMode(playerGameModePacket.GameMode);
    }

    /// <summary>What this player has left, which is the bar along the bottom of the screen and nothing more.</summary>
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

    /// <summary>
    /// Being put back at the spawn. The one thing the server ever does to a body this side simulates, and it
    /// only happens on a death, when what the client thought it was doing has stopped being true.
    /// </summary>
    public void ProcessPlayerRespawnPacket(PlayerRespawnPacket playerRespawnPacket)
    {
        _game.ClientPlayer.RespawnAt(playerRespawnPacket.SpawnPosition);
    }

    /// <summary>A stack lying on the ground that has come into view.</summary>
    public void ProcessItemSpawnPacket(ItemSpawnPacket itemSpawnPacket)
    {
        if (_game.World.LoadedEntities.ContainsKey(itemSpawnPacket.EntityID))
        {
            return;
        }

        var stack = new ItemStack(
            BlockRegistry.GetBlockFromIdentifier(itemSpawnPacket.BlockId),
            itemSpawnPacket.Count);

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

    /// <summary>
    /// Something this player has just walked over. The server has already taken it out of the world; what
    /// arrives here is what it was, since the inventory it goes into lives on this side alone.
    /// </summary>
    public void ProcessItemPickupPacket(ItemPickupPacket itemPickupPacket)
    {
        // Dropped here as well as by the despawn that follows, so the thing being collected stops being
        // drawn on the frame the sound plays rather than a tenth of a second later.
        if (_game.World.LoadedEntities.ContainsKey(itemPickupPacket.EntityID))
        {
            _game.World.DespawnEntity(itemPickupPacket.EntityID);
        }

        var picked = new ItemStack(
            BlockRegistry.GetBlockFromIdentifier(itemPickupPacket.BlockId),
            itemPickupPacket.Count);

        _game.ClientPlayer.Inventory.TryAdd(picked);
        _game.SoundDirector.OnItemPickedUp(_game.ClientPlayer.Position);
    }

    public void ProcessPlayerFellPacket(PlayerFellPacket playerFellPacket)
    {
        throw new InvalidOperationException("A client does not receive falls; it is the one that reports them.");
    }

    public void ProcessPlayerDropItemPacket(PlayerDropItemPacket playerDropItemPacket)
    {
        throw new InvalidOperationException("A client does not receive drops; it is the one that throws them.");
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

        // Before the world is drawn even once, so the hotbar is never seen full for a frame in a world that
        // is played empty handed.
        _game.ClientPlayer.SetGameMode(playerJoinAcceptPacket.GameMode);
        _game.ClientPlayer.SetHealth(playerJoinAcceptPacket.Health);

        _session.State = SessionState.Accepted;

        _game.World.Environment.CurrentTime = playerJoinAcceptPacket.CurrentTime;

        _game.World.SpawnEntity(_game.ClientPlayer);

        // Neither the seed nor the fact that a hosted world is open to other players is shown anywhere else,
        // and both are worth knowing, so they are said once on the way in. The seed comes from the world
        // rather than from what was asked for, since an existing world keeps its own.
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
        }
        else
        {
            Logger.Info("Player " + playerLeavePacket.ID + " left for reason " + playerLeavePacket.Reason + " with message " + playerLeavePacket.Message);

            // The name only lives on the entity, which is about to be despawned.
            if (_game.World.LoadedEntities.TryGetValue(playerLeavePacket.ID, out Entity? leavingEntity) &&
                leavingEntity is OtherClientPlayer leavingPlayer)
            {
                _game.MasterRenderer.IngameCanvas.AddSystemMessage(leavingPlayer.Name + " left the game");
            }
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
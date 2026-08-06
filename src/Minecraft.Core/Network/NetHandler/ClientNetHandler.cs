using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
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

    public void ProcessChatPacket(ChatPacket chatPacket)
    {
        _game.MasterRenderer.IngameCanvas.AddUserMessage(chatPacket.Sender, chatPacket.Message);
    }

    public void ProcessChunkDataPacket(ChunkDataPacket chunkDataPacket)
    {
        _game.World.AddPlayerPresenceToChunk(chunkDataPacket.Chunk);
    }

    public void ProcessExplosionPacket(ExplosionPacket explosionPacket)
    {
        _game.SoundDirector.OnExplosion(explosionPacket.Position);
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

        if (entity is not OtherClientPlayer && entity is not Mob)
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

    public void ProcessJoinRequestPacket(PlayerJoinRequestPacket playerJoinRequestPacket)
    {
        throw new InvalidOperationException();
    }

    public void ProcessJoinAcceptPacket(PlayerJoinAcceptPacket playerJoinAcceptPacket)
    {
        Logger.Info("You: " + playerJoinAcceptPacket.Name + " connected.");

        _game.ClientPlayer.ID = playerJoinAcceptPacket.PlayerID;
        _game.ClientPlayer.Name = playerJoinAcceptPacket.Name;
        _game.ClientPlayer.Position = playerJoinAcceptPacket.SpawnPosition;
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
}
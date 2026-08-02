using Minecraft.Core.Entities;
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

    public void ProcessPlayerDataPacket(PlayerDataPacket playerDataPacket)
    {
        if (!_game.World.LoadedEntities.TryGetValue(playerDataPacket.EntityID, out Entity? player))
        {
            Logger.Error("Received positional data for unregistered player " + playerDataPacket.EntityID);
            return;
        }
        if (!(player is OtherClientPlayer))
        {
            Logger.Error("Something else than other player stored in players map: " + player.GetType());
            return;
        }
        OtherClientPlayer otherPlayer = (OtherClientPlayer)player;
        otherPlayer.ServerPosition = playerDataPacket.Position;
        otherPlayer.ServerYaw = playerDataPacket.Yaw;
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
    }

    public void ProcessPlayerJoinPacket(PlayerJoinPacket playerJoinPacket)
    {
        OtherClientPlayer otherPlayer = new(playerJoinPacket.PlayerID, playerJoinPacket.Name, _game.World);
        UICanvasEntityName playerNameCanvas = new(_game, otherPlayer, playerJoinPacket.Name);
        _game.MasterRenderer.AddCanvas(playerNameCanvas);
        _game.World.SpawnEntity(otherPlayer);
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
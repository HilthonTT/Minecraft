using Minecraft.Core.Games;

namespace Minecraft.Core.Worlds;

public sealed class WorldClient : World
{
    public WorldClient(Game game) : base(game)
    {
        OnBlockPlacedHandler += game.MasterRenderer.Chunks.OnBlockPlaced;
        OnBlockRemovedHandler += game.MasterRenderer.Chunks.OnBlockRemoved;
        OnBlockPlacedHandler += game.SoundDirector.OnBlockPlaced;
        OnBlockRemovedHandler += game.SoundDirector.OnBlockRemoved;
        OnBlockRemovedHandler += game.MasterRenderer.Particles.OnBlockRemoved;
        OnChunkLoadedHandler += game.MasterRenderer.Chunks.OnChunkLoaded;
        OnChunkUnloadedHandler += game.MasterRenderer.Chunks.OnChunkUnloaded;
    }
}

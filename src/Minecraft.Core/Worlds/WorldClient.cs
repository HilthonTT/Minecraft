using Minecraft.Core.Games;

namespace Minecraft.Core.Worlds;

/// <summary>
/// The representation of the world used on the client.
/// </summary>
public sealed class WorldClient : World
{
    public WorldClient(Game game) : base(game)
    {
        OnBlockPlacedHandler += game.MasterRenderer.OnBlockPlaced;
        OnBlockRemovedHandler += game.MasterRenderer.OnBlockRemoved;
        OnBlockPlacedHandler += game.SoundDirector.OnBlockPlaced;
        OnBlockRemovedHandler += game.SoundDirector.OnBlockRemoved;
        OnBlockRemovedHandler += game.MasterRenderer.Particles.OnBlockRemoved;
        OnChunkLoadedHandler += game.MasterRenderer.OnChunkLoaded;
        OnChunkUnloadedHandler += game.MasterRenderer.OnChunkUnloaded;
    }
}
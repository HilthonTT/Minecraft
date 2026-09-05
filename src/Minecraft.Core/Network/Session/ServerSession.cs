using Minecraft.Core.Network.NetHandler;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Network.Session;

public sealed class ServerSession : Session
{
    private readonly ChunkProvider _chunkProvider;
    private readonly EntityTracker _entityTracker;

    public ServerSession(Connection connection, INetHandler netHandler)
        : base(connection, netHandler)
    {
        _chunkProvider = new ChunkProvider(this);
        _entityTracker = new EntityTracker(this);
    }

    public void Update(float deltaTimeSeconds)
    {
        _chunkProvider.Update();
        _entityTracker.Update(deltaTimeSeconds);
    }

    public void ReleaseWorldPresence()
    {
        if (Player?.World is { } world)
        {
            _chunkProvider.ReleaseAll(world);
        }
    }
}

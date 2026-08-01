using Minecraft.Core.Network.NetHandler;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Network.Session;

public sealed class ServerSession : Session
{
    private readonly ChunkProvider _chunkProvider;

    public ServerSession(Connection connection, INetHandler netHandler)
        : base(connection, netHandler)
    {
        _chunkProvider = new ChunkProvider(this);
    }

    public void Update(float deltaTimeSeconds)
    {
        _chunkProvider.Update(deltaTimeSeconds);
    }
}
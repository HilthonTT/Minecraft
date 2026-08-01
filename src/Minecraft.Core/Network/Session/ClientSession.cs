using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Session;

public sealed class ClientSession : Session
{
    public ClientSession(Connection connection, INetHandler netHandler)
        : base(connection, netHandler)
    {
    }
}

using OpenTK.Mathematics;
using Minecraft.Core.Worlds;

namespace Minecraft.Core.Entities.Player;

public sealed class ServerPlayer : Player
{
    public ServerPlayer(int id, string playerName, World? world, Vector3 position)
        : base(id, playerName, world, position)
    {
    }
}

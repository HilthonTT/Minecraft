using OpenTK.Mathematics;
using Minecraft.Core.Worlds;

namespace Minecraft.Core.Entities.Player;

public sealed class OtherClientPlayer : Player
{
    public OtherClientPlayer(int id, string playerName, World? world) : base(id, playerName, world, Vector3.Zero)
    {
    }

    public override void Update(float deltaTime, World world)
    {
        InterpolateTowardsServerState(deltaTime);
        base.Update(deltaTime, world);
    }
}

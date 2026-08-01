using Minecraft.Core.Utilities;
using OpenTK.Mathematics;
using Minecraft.Core.Worlds;

namespace Minecraft.Core.Entities.Player;

public sealed class OtherClientPlayer : Player
{
    public Vector3 ServerPosition { get; set; }
    private const float PositionLerpSmoothFactor = 20;

    public OtherClientPlayer(int id, string playerName, World? world) : base(id, playerName, world, Vector3.Zero)
    {
    }

    public override void Update(float deltaTime, World world)
    {
        Position = MathUtils.Lerp(Position, ServerPosition, deltaTime * PositionLerpSmoothFactor);
        base.Update(deltaTime, world);
    }
}
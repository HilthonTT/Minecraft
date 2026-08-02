using Minecraft.Core.Utilities;
using OpenTK.Mathematics;
using Minecraft.Core.Worlds;

namespace Minecraft.Core.Entities.Player;

public sealed class OtherClientPlayer : Player
{
    public Vector3 ServerPosition { get; set; }

    /// <summary>The yaw last reported by the server, which the rendered yaw catches up to.</summary>
    public float ServerYaw { get; set; }

    private const float PositionLerpSmoothFactor = 20;

    public OtherClientPlayer(int id, string playerName, World? world) : base(id, playerName, world, Vector3.Zero)
    {
    }

    public override void Update(float deltaTime, World world)
    {
        // Positions arrive an order of magnitude less often than frames are drawn, so both the position and
        // the facing are eased towards the last one received rather than snapping to it.
        Position = MathUtils.Lerp(Position, ServerPosition, deltaTime * PositionLerpSmoothFactor);
        Yaw = MathUtils.LerpAngle(Yaw, ServerYaw, deltaTime * PositionLerpSmoothFactor);
        base.Update(deltaTime, world);
    }
}

using Minecraft.Core.Entities.Player;
using Minecraft.Core.Physics;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public sealed class PlayerHoverBlockRenderer
{
    private const int RestingLineWidth = 3;
    private const int BreakingLineWidth = 7;

    private static readonly Vector3 RestingColor = Vector3.Zero;

    private static readonly Vector3 BreakingColor = new(1.0F, 0.92F, 0.62F);

    private readonly WireframeRenderer _wireframeRenderer;
    private readonly ClientPlayer _player;

    public PlayerHoverBlockRenderer(WireframeRenderer wireframeRenderer, ClientPlayer player)
    {
        _wireframeRenderer = wireframeRenderer;
        _player = player;
    }

    public void RenderSelection()
    {
        if (_player.MouseOverObject is null)
        {
            return;
        }

        var blockPos = _player.MouseOverObject.IntersectedBlockPos;
        BlockState state = _player.MouseOverObject.BlockstateHit;

        float progress = Math.Clamp(_player.BreakProgress, 0F, 1F);
        Vector3 color = RestingColor + ((BreakingColor - RestingColor) * progress);
        var lineWidth = (int)MathF.Round(RestingLineWidth + ((BreakingLineWidth - RestingLineWidth) * progress));

        foreach (AxisAlignedBox aabb in state.GetBlock().GetSelectionBox(state, blockPos))
        {
            Vector3 scaleVector = new(aabb.Max.X - aabb.Min.X, aabb.Max.Y - aabb.Min.Y, aabb.Max.Z - aabb.Min.Z);
            Vector3 translation = aabb.Min;
            _wireframeRenderer.RenderWireframeAt(lineWidth, translation, scaleVector, color);
        }
    }
}

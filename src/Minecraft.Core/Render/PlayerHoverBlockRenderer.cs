using Minecraft.Core.Entities.Player;
using Minecraft.Core.Physics;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

/// <summary>
/// Outlines the block under the crosshair, and says how far through breaking it the player is.
/// <para>
/// The outline is the progress bar. A block being dug out has nowhere else to put one: a bar somewhere on the
/// interface would be read a long way from the thing it is about, and the block itself is exactly where the
/// player is already looking.
/// </para>
/// </summary>
public sealed class PlayerHoverBlockRenderer
{
    /// <summary>How thick the outline is drawn at rest, and at the moment the block gives way.</summary>
    private const int RestingLineWidth = 3;
    private const int BreakingLineWidth = 7;

    /// <summary>Black at rest, so the outline reads as a line drawn around the block rather than as a glow.</summary>
    private static readonly Vector3 RestingColor = Vector3.Zero;

    /// <summary>What it has come up to by the time the block is about to go.</summary>
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

        // Brightens and thickens as the block comes apart, so a long dig reads as getting somewhere. An
        // instant break passes through this in a single frame and simply never shows it.
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

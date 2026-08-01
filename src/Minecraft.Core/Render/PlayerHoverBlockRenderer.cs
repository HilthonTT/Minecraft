using Minecraft.Core.Entities.Player;
using Minecraft.Core.Physics;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public sealed class PlayerHoverBlockRenderer
{
    private readonly WireframeRenderer _wireframeRenderer;
    private readonly ClientPlayer _player;

    public PlayerHoverBlockRenderer(WireframeRenderer wireframeRenderer, ClientPlayer player)
    {
        _wireframeRenderer = wireframeRenderer;
        _player = player;
    }

    public void RenderSelection()
    {
        if (_player.MouseOverObject == null)
        {
            return;
        }

        var blockPos = _player.MouseOverObject.IntersectedBlockPos;
        BlockState state = _player.MouseOverObject.BlockstateHit;

        foreach (AxisAlignedBox aabb in state.GetBlock().GetSelectionBox(state, blockPos))
        {
            Vector3 scaleVector = new(aabb.Max.X - aabb.Min.X, aabb.Max.Y - aabb.Min.Y, aabb.Max.Z - aabb.Min.Z);
            Vector3 translation = aabb.Min;
            _wireframeRenderer.RenderWireframeAt(3, translation, scaleVector, Vector3.Zero);
        }
    }
}
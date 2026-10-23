using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// The boxes the partial blocks are made of. The model draws them and the block collides and is selected with
/// them, so what a player sees is what they walk into and what they point at.
/// </summary>
public static class BlockShapes
{
    private const float FenceCollisionHeight = 1.5F;

    public static Box[] Slab(bool top) =>
        [top ? Box.FromSixteenths(0, 8, 0, 16, 16, 16) : Box.FromSixteenths(0, 0, 0, 16, 8, 16)];

    public static Box[] Stairs(Direction facing, bool upsideDown)
    {
        // Drawn facing front: the bottom step runs along the front, the tall half along the back.
        Box step = upsideDown ? Box.FromSixteenths(0, 8, 0, 16, 16, 16) : Box.FromSixteenths(0, 0, 0, 16, 8, 16);
        Box riser = upsideDown ? Box.FromSixteenths(0, 0, 0, 16, 8, 8) : Box.FromSixteenths(0, 8, 0, 16, 16, 8);

        return [step.RotatedToFace(facing), riser.RotatedToFace(facing)];
    }

    public static Box[] Door(Direction panelSide) =>
        [Box.FromSixteenths(0, 0, 13, 16, 16, 16).RotatedToFace(panelSide)];

    public static Box[] Ladder(Direction attachment) =>
        [Box.FromSixteenths(0, 0, 15, 16, 16, 16).RotatedToFace(attachment)];

    public static Box[] Fence(BlockStateConnecting state, float height = 1F) =>
        Connected(state, thickness: 4F, height, postAlways: true);

    public static Box[] FenceCollision(BlockStateConnecting state) => Fence(state, FenceCollisionHeight);

    public static Box[] Pane(BlockStateConnecting state)
    {
        if (state.HasNoConnections)
        {
            // A pane on its own is drawn as two crossed sheets, as if it reached every way.
            return
            [
                Box.FromSixteenths(7, 0, 0, 9, 16, 16),
                Box.FromSixteenths(0, 0, 7, 16, 16, 9),
            ];
        }

        return Connected(state, thickness: 2F, height: 1F, postAlways: false);
    }

    /// <summary>
    /// A post in the middle and an arm out to each connected side. The post is left out of a pane, where the arms
    /// meet in the middle anyway and a post would only be a second skin over them.
    /// </summary>
    private static Box[] Connected(BlockStateConnecting state, float thickness, float height, bool postAlways)
    {
        float low = (16F - thickness) / 2F;
        float high = low + thickness;
        float top = height * 16F;

        List<Box> boxes = [];

        if (postAlways)
        {
            boxes.Add(Box.FromSixteenths(low, 0, low, high, top, high));
        }

        int connections = BlockStateConnecting.Sides.Count(state.ConnectsTowards);

        foreach (Direction side in BlockStateConnecting.Sides)
        {
            // A pane joined on one side only carries on out of the other, rather than stopping in mid-air.
            bool reaches = state.ConnectsTowards(side) ||
                           (!postAlways && connections == 1 && state.ConnectsTowards(Opposite(side)));
            if (!reaches)
            {
                continue;
            }

            // Drawn reaching to the front, from the middle of the cell to its edge.
            Box arm = postAlways
                ? Box.FromSixteenths(low + 1, 0, high, high - 1, top, 16)
                : Box.FromSixteenths(low, 0, 8, high, top, 16);

            boxes.Add(arm.RotatedToFace(side));
        }

        return [.. boxes];
    }

    private static Direction Opposite(Direction side) => side switch
    {
        Direction.Back => Direction.Front,
        Direction.Front => Direction.Back,
        Direction.Left => Direction.Right,
        _ => Direction.Left,
    };

    public static AxisAlignedBox[] InWorld(Box[] boxes, Vector3i blockPos)
    {
        var placed = new AxisAlignedBox[boxes.Length];
        var origin = new Vector3(blockPos.X, blockPos.Y, blockPos.Z);

        for (int i = 0; i < boxes.Length; i++)
        {
            placed[i] = new AxisAlignedBox(origin + boxes[i].Min, origin + boxes[i].Max);
        }

        return placed;
    }
}

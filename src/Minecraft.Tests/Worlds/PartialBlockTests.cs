using Minecraft.Core.Inventories.Items;
using Minecraft.Core.IO;
using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;
using Minecraft.Core.Worlds.Blocks.Types;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Worlds;

[Collection(RegistryCollection.Name)]
public sealed class PartialBlockTests
{
    private const float Tolerance = 0.0001F;

    private static T RoundTrip<T>(T state) where T : BlockState
    {
        using var memory = new MemoryStream();
        using (var buffered = new BufferedStream(memory))
        {
            var writer = new BufferedDataStream(buffered);
            state.ToStream(writer);
            writer.Flush();
        }

        byte[] bytes = memory.ToArray();
        int head = 0;

        ushort blockId = DataConverter.BytesToUInt16(bytes, ref head);
        BlockState read = BlockRegistry.GetState(BlockRegistry.GetBlockFromIdentifier(blockId));
        read.ExtractFromByteStream(bytes, ref head);

        Assert.Equal(bytes.Length, head);
        Assert.Equal(state.PayloadSize(), bytes.Length - sizeof(ushort));
        return Assert.IsType<T>(read);
    }

    [Fact]
    public void AnOpenUpperDoorIsSavedAsOne()
    {
        var door = new BlockStateDoor(BlockRegistry.Door) { Facing = Direction.Left, IsOpen = true, IsUpper = true };

        BlockStateDoor read = RoundTrip(door);

        Assert.Equal(Direction.Left, read.Facing);
        Assert.True(read.IsOpen);
        Assert.True(read.IsUpper);
    }

    [Fact]
    public void UpsideDownStairsKeepWhichWayTheyFace()
    {
        var stairs = new BlockStateStairs(BlockRegistry.CobblestoneStairs) { Facing = Direction.Right, IsUpsideDown = true };

        BlockStateStairs read = RoundTrip(stairs);

        Assert.Same(BlockRegistry.CobblestoneStairs, read.GetBlock());
        Assert.Equal(Direction.Right, read.Facing);
        Assert.True(read.IsUpsideDown);
    }

    [Fact]
    public void ATopSlabAndALadderOnItsWallComeBackTheSame()
    {
        Assert.True(RoundTrip(new BlockStateSlab(BlockRegistry.PlanksSlab) { IsTop = true }).IsTop);
        Assert.Equal(
            Direction.Back,
            RoundTrip(new BlockStateLadder(BlockRegistry.Ladder) { Attachment = Direction.Back }).Attachment);
    }

    [Fact]
    public void AFenceRemembersWhatItReachesTo()
    {
        var fence = new BlockStateConnecting(BlockRegistry.Fence);
        fence.SetConnections(side => side is Direction.Front or Direction.Left);

        BlockStateConnecting read = RoundTrip(fence);

        Assert.True(read.ConnectsTowards(Direction.Front));
        Assert.True(read.ConnectsTowards(Direction.Left));
        Assert.False(read.ConnectsTowards(Direction.Back));
        Assert.False(read.ConnectsTowards(Direction.Right));
    }

    [Fact]
    public void BitsAStateDoesNotUseAreDroppedOnTheWayIn()
    {
        byte[] bytes = [0xFF];
        int head = 0;

        var slab = new BlockStateSlab(BlockRegistry.StoneSlab);
        slab.ExtractFromByteStream(bytes, ref head);

        Assert.Equal(1, slab.Variant);
    }

    [Theory]
    [InlineData(Direction.Front, 0.5F, 1F)]
    [InlineData(Direction.Back, 0.5F, 0F)]
    [InlineData(Direction.Left, 0F, 0.5F)]
    [InlineData(Direction.Right, 1F, 0.5F)]
    public void TurningAShapeDrawnFacingFrontPointsItTheOtherWay(Direction facing, float edgeX, float edgeZ)
    {
        // A sliver along the front edge of the cell, turned to face each side in turn, ends up on that side.
        Box sliver = Box.FromSixteenths(7, 0, 15, 9, 16, 16).RotatedToFace(facing);
        Vector3 centre = (sliver.Min + sliver.Max) / 2F;

        Assert.Equal(edgeX, MathF.Round(centre.X * 2F) / 2F, Tolerance);
        Assert.Equal(edgeZ, MathF.Round(centre.Z * 2F) / 2F, Tolerance);
    }

    [Fact]
    public void StairsRiseOnTheSideAwayFromTheirStep()
    {
        Box[] boxes = BlockShapes.Stairs(Direction.Front, upsideDown: false);
        Box riser = boxes.Single(box => box.Min.Y >= 0.5F - Tolerance);

        // The bottom step faces front, so the tall half is at the back, the low-Z half of the cell.
        Assert.Equal(0F, riser.Min.Z, Tolerance);
        Assert.Equal(0.5F, riser.Max.Z, Tolerance);
    }

    [Fact]
    public void AnOpenDoorSwingsToTheNeighbouringSide()
    {
        var door = new BlockStateDoor(BlockRegistry.Door) { Facing = Direction.Front };
        Assert.Equal(Direction.Front, door.PanelSide);

        door.IsOpen = true;
        Assert.NotEqual(Direction.Front, door.PanelSide);
        Assert.NotEqual(Direction.Back, door.PanelSide);
    }

    [Fact]
    public void TwoSlabsMakeTheBlockTheyWereCutFrom()
    {
        var slab = (BlockSlab)BlockRegistry.CobblestoneSlab;
        var bottom = new BlockStateSlab(slab);
        var top = new BlockStateSlab(slab) { IsTop = true };

        Assert.Same(BlockRegistry.Cobblestone, slab.FullBlock);
        Assert.True(slab.CompletesSlab(bottom, top));
        Assert.False(slab.CompletesSlab(bottom, new BlockStateSlab(slab)));
        Assert.False(slab.CompletesSlab(bottom, new BlockStateSlab(BlockRegistry.PlanksSlab) { IsTop = true }));
    }

    [Fact]
    public void ASlabDigsLikeTheBlockItIsHalfOf()
    {
        Assert.Equal(BlockRegistry.Stone.SecondsToBreak, BlockRegistry.StoneSlab.SecondsToBreak);
        Assert.Equal(BlockRegistry.Stone.HarvestTool, BlockRegistry.StoneSlab.HarvestTool);
        Assert.True(BlockRegistry.StoneSlab.RequiresCorrectTool);
        Assert.Equal(ToolKind.Axe, BlockRegistry.PlanksStairs.HarvestTool);
    }

    [Fact]
    public void OnlyTheLadderIsClimbed()
    {
        for (int id = 1; id <= BlockRegistry.Count; id++)
        {
            Block block = BlockRegistry.GetBlockFromIdentifier(id);
            Assert.Equal(block == BlockRegistry.Ladder, block.IsClimbable);
        }
    }
}

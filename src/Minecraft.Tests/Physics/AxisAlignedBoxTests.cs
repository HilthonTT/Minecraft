using Minecraft.Core.Physics;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Physics;

public sealed class AxisAlignedBoxTests
{
    private static AxisAlignedBox UnitCube() => new(Vector3.Zero, Vector3.One);

    [Fact]
    public void TwoBoxesThatOverlapIntersect()
    {
        AxisAlignedBox block = UnitCube();
        var overlapping = new AxisAlignedBox(new Vector3(0.5F, 0.5F, 0.5F), new Vector3(1.5F, 1.5F, 1.5F));

        Assert.True(block.Intersects(overlapping));
        Assert.True(overlapping.Intersects(block));
    }

    [Fact]
    public void TwoBoxesMeetingFaceToFaceDoNot()
    {
        AxisAlignedBox floor = UnitCube();
        var standingOn = new AxisAlignedBox(new Vector3(0, 1, 0), new Vector3(1, 2, 1));

        Assert.False(floor.Intersects(standingOn));
    }

    [Fact]
    public void BoxesApartInAnySingleAxisDoNotIntersect()
    {
        AxisAlignedBox block = UnitCube();

        Assert.False(block.Intersects(new AxisAlignedBox(new Vector3(2, 0, 0), new Vector3(3, 1, 1))));
        Assert.False(block.Intersects(new AxisAlignedBox(new Vector3(0, 2, 0), new Vector3(1, 3, 1))));
        Assert.False(block.Intersects(new AxisAlignedBox(new Vector3(0, 0, 2), new Vector3(1, 1, 3))));
    }

    [Fact]
    public void ARayPointedAtABoxReportsHowFarAwayItIs()
    {
        var box = new AxisAlignedBox(new Vector3(2, -1, -1), new Vector3(3, 1, 1));
        var ray = new Ray(Vector3.Zero, Vector3.UnitX);

        Assert.Equal(2F, box.Intersects(ray), 4);
    }

    [Fact]
    public void ARayPointedElsewhereReportsNothing()
    {
        var box = new AxisAlignedBox(new Vector3(2, -1, -1), new Vector3(3, 1, 1));
        var pastIt = new Ray(Vector3.Zero, Vector3.UnitY);

        Assert.Equal(float.MaxValue, box.Intersects(pastIt));
    }

    [Fact]
    public void ABoxBehindTheRayIsNotSomethingItHits()
    {
        var behind = new AxisAlignedBox(new Vector3(-3, -1, -1), new Vector3(-2, 1, 1));
        var ray = new Ray(Vector3.Zero, Vector3.UnitX);

        Assert.Equal(float.MaxValue, behind.Intersects(ray));
    }

    [Fact]
    public void TheNormalIsTheFaceTheRayCameInThrough()
    {
        AxisAlignedBox block = UnitCube();

        Assert.Equal(-Vector3.UnitX, block.GetNormalAtIntersectionPoint(new Vector3(0, 0.5F, 0.5F)));
        Assert.Equal(Vector3.UnitX, block.GetNormalAtIntersectionPoint(new Vector3(1, 0.5F, 0.5F)));
        Assert.Equal(Vector3.UnitY, block.GetNormalAtIntersectionPoint(new Vector3(0.5F, 1, 0.5F)));
        Assert.Equal(-Vector3.UnitZ, block.GetNormalAtIntersectionPoint(new Vector3(0.5F, 0.5F, 0)));
    }

    [Fact]
    public void AllEightCornersAreTheCornersOfTheBox()
    {
        Vector3[] corners = UnitCube().GetAllCorners();

        Assert.Equal(8, corners.Length);
        Assert.Equal(8, corners.Distinct().Count());
        Assert.All(corners, corner =>
        {
            Assert.True(corner.X is 0 or 1);
            Assert.True(corner.Y is 0 or 1);
            Assert.True(corner.Z is 0 or 1);
        });
    }

    [Fact]
    public void ABoxCanBeMovedWithoutBeingRebuilt()
    {
        AxisAlignedBox box = UnitCube();

        box.SetDimensions(new Vector3(4, 5, 6), new Vector3(5, 7, 7));

        Assert.Equal(new Vector3(4, 5, 6), box.Min);
        Assert.Equal(new Vector3(5, 7, 7), box.Max);
    }
}

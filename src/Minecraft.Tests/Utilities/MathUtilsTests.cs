using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Utilities;

public sealed class MathUtilsTests
{
    [Fact]
    public void DegreesAndRadiansAreEachOthersUndoing()
    {
        Assert.Equal(MathF.PI, MathUtils.DegreeToRadian(180), 5);
        Assert.Equal(90F, MathUtils.RadianToDegree(MathF.PI / 2), 4);
        Assert.Equal(37F, MathUtils.RadianToDegree(MathUtils.DegreeToRadian(37)), 3);
    }

    [Fact]
    public void ALookVectorIsAlwaysAUnitOne()
    {
        foreach (float yaw in new[] { 0F, 1F, -2.5F, MathF.PI })
        {
            foreach (float pitch in new[] { 0F, 0.7F, -1.2F })
            {
                Assert.Equal(1F, MathUtils.CreateLookAtVector(yaw, pitch).Length, 4);
            }
        }
    }

    [Fact]
    public void LookingStraightUpAndStraightAheadPointWhereTheyShould()
    {
        Vector3 up = MathUtils.CreateLookAtVector(0, MathF.PI / 2);
        Vector3 ahead = MathUtils.CreateLookAtVector(0, 0);

        Assert.Equal(1F, up.Y, 4);
        Assert.Equal(1F, ahead.Z, 4);
        Assert.Equal(0F, ahead.Y, 4);
    }

    [Fact]
    public void AnglesTurnTheShorterWayRound()
    {
        float from = MathF.Tau - 0.1F;
        float stepped = MathUtils.LerpAngle(from, 0.1F, 0.5F);

        Assert.Equal(MathF.Tau, stepped, 4);
    }

    [Fact]
    public void InterpolatingAnAngleWithItselfStaysPut()
    {
        Assert.Equal(1.3F, MathUtils.LerpAngle(1.3F, 1.3F, 0.5F), 4);
        Assert.Equal(1.3F, MathUtils.LerpAngle(1.3F, 2.7F, 0F), 4);
    }

    [Fact]
    public void LerpingAVectorWalksFromOneEndToTheOther()
    {
        var from = new Vector3(0, 0, 0);
        var to = new Vector3(10, -20, 4);

        Assert.Equal(from, MathUtils.Lerp(from, to, 0));
        Assert.Equal(to, MathUtils.Lerp(from, to, 1));
        Assert.Equal(new Vector3(5, -10, 2), MathUtils.Lerp(from, to, 0.5F));
    }

    [Theory]
    [InlineData(0F, 0F)]
    [InlineData(1F, 100F)]
    [InlineData(0.25F, 25F)]
    [InlineData(2F, 200F)]
    public void ConvertingBetweenRangesKeepsTheBoundariesAndWhatIsBetweenThem(float value, float expected)
    {
        Assert.Equal(expected, MathUtils.ConvertRange(0, 1, 0, 100, value), 3);
    }

    [Fact]
    public void ARangeCanRunBackwards()
    {
        Assert.Equal(1F, MathUtils.ConvertRange(0, 1, 1, -1, 0), 3);
        Assert.Equal(-1F, MathUtils.ConvertRange(0, 1, 1, -1, 1), 3);
    }
}

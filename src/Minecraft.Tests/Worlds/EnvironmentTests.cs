using Environment = Minecraft.Core.Worlds.Environment;

namespace Minecraft.Tests.Worlds;

public sealed class EnvironmentTests
{
    private const int DayLength = 2400;

    [Theory]
    [InlineData(-100F, 2300F)]
    [InlineData(2400F, 0F)]
    [InlineData(5000F, 200F)]
    [InlineData(812.5F, 812.5F)]
    public void ATimeOutsideTheDayIsBroughtBackIntoIt(float stored, float expected)
    {
        var environment = new Environment(DayLength) { CurrentTime = stored };

        Assert.Equal(expected, environment.CurrentTime, 3);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ATimeThatIsNotANumberStillLandsSomewhereInTheDay(float stored)
    {
        var environment = new Environment(DayLength) { CurrentTime = stored };

        Assert.InRange(environment.CurrentTime, 0F, DayLength - 1);
    }

    [Theory]
    [InlineData(-100F)]
    [InlineData(2400F)]
    [InlineData(99999F)]
    [InlineData(float.NaN)]
    public void TheSkyCanBeDrawnWhateverTimeWasStored(float stored)
    {
        var environment = new Environment(DayLength) { CurrentTime = stored };

        environment.GetCurrentTopSkyColor();
        environment.GetCurrentHorizonColor();
        environment.GetCurrentMoonGlowColor();
    }

    [Fact]
    public void TheDayRollsOverWithoutLosingTheTimePastMidnight()
    {
        var environment = new Environment(DayLength) { CurrentTime = DayLength - 1 };

        environment.Update(3F);

        Assert.Equal(2F, environment.CurrentTime, 3);
    }
}

using Minecraft.Core.Games;

namespace Minecraft.Tests.Games;

public sealed class GameSettingsTests
{
    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("1e40")]
    public void NonFiniteNumbersAreIgnored(string value)
    {
        var settings = new GameSettings();

        settings.Apply("fov", value);
        settings.Apply("volume", value);
        settings.Apply("sensitivity", value);

        Assert.Equal(GameSettings.DefaultFieldOfViewDegrees, settings.FieldOfViewDegrees);
        Assert.Equal(1.0F, settings.MasterVolume);
        Assert.Equal(1.0F, settings.MouseSensitivity);
    }

    [Fact]
    public void FiniteNumbersAreApplied()
    {
        var settings = new GameSettings();

        settings.Apply("fov", "70");
        settings.Apply("volume", "0.5");
        settings.Apply("sensitivity", "2");

        Assert.Equal(70F, settings.FieldOfViewDegrees);
        Assert.Equal(0.5F, settings.MasterVolume);
        Assert.Equal(2F, settings.MouseSensitivity);
    }
}

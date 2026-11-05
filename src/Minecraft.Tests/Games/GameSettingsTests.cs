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

    [Fact]
    public void RealisticShadersAreOffByDefault()
    {
        Assert.False(new GameSettings().RealisticShaders);
    }

    [Theory]
    [InlineData("realistic", true)]
    [InlineData("REALISTIC", true)]
    [InlineData("off", false)]
    public void ShadersSettingIsApplied(string value, bool expected)
    {
        var settings = new GameSettings { RealisticShaders = !expected };

        settings.Apply("shaders", value);

        Assert.Equal(expected, settings.RealisticShaders);
    }

    [Fact]
    public void UnknownShadersValueIsIgnored()
    {
        var settings = new GameSettings { RealisticShaders = true };

        settings.Apply("shaders", "ultra");

        Assert.True(settings.RealisticShaders);
    }

    [Fact]
    public void ChangingShadersRaisesTheChangedEvent()
    {
        var settings = new GameSettings();
        int changes = 0;
        settings.OnChangedHandler += () => changes++;

        settings.RealisticShaders = true;
        settings.RealisticShaders = true;
        settings.RealisticShaders = false;

        Assert.Equal(2, changes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShadersSettingSurvivesSaveAndLoad(bool realistic)
    {
        string path = Path.Combine(Path.GetTempPath(), "minecraft-options-" + Guid.NewGuid().ToString("N") + ".txt");

        try
        {
            var saved = new GameSettings
            {
                RealisticShaders = realistic,
                FieldOfViewDegrees = 75F,
            };
            saved.Save(path);

            GameSettings loaded = GameSettings.Load(path);

            Assert.Equal(realistic, loaded.RealisticShaders);
            Assert.Equal(75F, loaded.FieldOfViewDegrees);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("125", 125)]
    [InlineData("10", GameSettings.MinGuiScalePercent)]
    [InlineData("999", GameSettings.MaxGuiScalePercent)]
    [InlineData("big", GameSettings.DefaultGuiScalePercent)]
    public void GuiScaleIsParsedAndClamped(string value, int expected)
    {
        var settings = new GameSettings();

        settings.Apply("guiscale", value);

        Assert.Equal(expected, settings.GuiScalePercent);
    }

    [Fact]
    public void ShowFpsIsParsed()
    {
        var settings = new GameSettings();

        settings.Apply("showfps", "true");

        Assert.True(settings.ShowFps);
    }
}

using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Render.UI;
using Minecraft.Core.Render.UI.Presets;

namespace Minecraft.Tests.Render;

[Collection(RegistryCollection.Name)]
public sealed class WorldSummaryTests
{
    private static readonly DateTime _now = new(2026, 11, 5, 15, 0, 0);

    [Fact]
    public void ParsesSeedAndGameModeFromLevelData()
    {
        WorldSummary summary = WorldSummary.Parse("Home", ["version=2", "seed=-42", "gamemode=creative"], null);

        Assert.Equal(-42, summary.Seed);
        Assert.Equal(GameMode.Creative, summary.GameMode);
    }

    [Fact]
    public void IgnoresFieldsItCannotRead()
    {
        WorldSummary summary = WorldSummary.Parse("Home", ["seed=lots", "gamemode=hardcore", "junk"], null);

        Assert.Null(summary.Seed);
        Assert.Null(summary.GameMode);
        Assert.Equal("No details saved", summary.Describe(_now));
    }

    [Fact]
    public void DescribesModeAgeAndSeed()
    {
        var summary = new WorldSummary("Home", GameMode.Survival, 7, _now.AddMinutes(-5));

        Assert.Equal("Survival  |  Played 5 minutes ago  |  Seed 7", summary.Describe(_now));
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(1, "1 minute ago")]
    [InlineData(59, "59 minutes ago")]
    [InlineData(120, "2 hours ago")]
    [InlineData(16 * 60, "yesterday")]
    [InlineData(3 * 24 * 60, "3 days ago")]
    [InlineData(60 * 24 * 60, "on 6 Sep 2026")]
    public void DescribesHowLongAgoAWorldWasPlayed(int minutesAgo, string expected)
    {
        Assert.Equal(expected, WorldSummary.DescribeAge(_now.AddMinutes(-minutesAgo), _now));
    }

    [Fact]
    public void ToolsDescribeTheirWear()
    {
        var pickaxe = new ItemStack(ItemRegistry.WoodenPickaxe, 1, damage: 3);

        string description = UIItemDescriptions.Describe(pickaxe, showCount: true);

        Assert.Contains("Durability " + (ItemRegistry.WoodenPickaxe.MaxDurability - 3) + " / " + ItemRegistry.WoodenPickaxe.MaxDurability, description);
        Assert.Contains("Attack damage", description);
    }

    [Fact]
    public void StacksDescribeTheirCountOnlyWhenAsked()
    {
        var sticks = new ItemStack(ItemRegistry.Stick, 12);

        Assert.Equal("12 of 64 in this stack", UIItemDescriptions.Describe(sticks, showCount: true));
        Assert.Equal(string.Empty, UIItemDescriptions.Describe(sticks, showCount: false));
    }
}

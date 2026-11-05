using Minecraft.Core.Render.UI;
using Minecraft.Core.Render.UI.Presets;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Render;

public sealed class UILayoutTests
{
    private static float Measure(string text) => text.Length * 10F;

    [Fact]
    public void ShortTextIsNotEllipsized()
    {
        Assert.Equal("abc", UITextLayout.Ellipsize("abc", 100F, Measure));
    }

    [Fact]
    public void LongTextIsCutWithAnEllipsis()
    {
        string fitted = UITextLayout.Ellipsize("abcdefghij", 60F, Measure);

        Assert.Equal("abc...", fitted);
        Assert.True(Measure(fitted) <= 60F);
    }

    [Fact]
    public void WrapBreaksOnSpaces()
    {
        List<string> lines = UITextLayout.Wrap("aa bb cc dd", 50F, Measure);

        Assert.Equal(["aa bb", "cc dd"], lines);
    }

    [Fact]
    public void WrapSplitsWordsLongerThanALine()
    {
        List<string> lines = UITextLayout.Wrap("abcdefgh", 30F, Measure);

        Assert.Equal(["abc", "def", "gh"], lines);
    }

    [Fact]
    public void WrapKeepsExistingLineBreaks()
    {
        Assert.Equal(["a", "b"], UITextLayout.Wrap("a\nb", 100F, Measure));
    }

    [Theory]
    [InlineData(-5F, 0)]
    [InlineData(4F, 0)]
    [InlineData(6F, 1)]
    [InlineData(24F, 2)]
    [InlineData(500F, 4)]
    public void IndexAtOffsetPicksTheNearestBoundary(float offset, int expected)
    {
        Assert.Equal(expected, UITextLayout.IndexAtOffset("abcd", offset, Measure));
    }

    [Fact]
    public void FirstVisibleIndexScrollsToKeepTheCaretInView()
    {
        int first = UITextLayout.FirstVisibleIndex("abcdefghij", 10, 0, 40F, Measure);

        Assert.Equal(6, first);
    }

    [Fact]
    public void FirstVisibleIndexScrollsBackWhenTextShrinks()
    {
        int first = UITextLayout.FirstVisibleIndex("abc", 3, 2, 40F, Measure);

        Assert.Equal(0, first);
    }

    [Theory]
    [InlineData(1280, 720, 100, 1F)]
    [InlineData(2560, 1440, 100, 2F)]
    [InlineData(1280, 720, 150, 1.5F)]
    [InlineData(500, 1000, 100, 0.6F)]
    [InlineData(200, 100, 100, 0.6F)]
    public void ScaleFollowsTheSmallerWindowFit(int width, int height, int percent, float expected)
    {
        Assert.Equal(expected, UIScale.ComputeFactor(width, height, percent), 3);
    }

    [Fact]
    public void ScalePreferenceIsClamped()
    {
        Assert.Equal(UIScale.ComputeFactor(1280, 720, 200), UIScale.ComputeFactor(1280, 720, 999));
    }

    [Fact]
    public void CanvasSizeIsTheWindowDividedByTheFactor()
    {
        Assert.Equal(new Vector2i(1280, 720), UIScale.ComputeCanvasSize(2560, 1440, 2F));
    }

    [Fact]
    public void ApproachMovesTowardsTheTargetAndSettles()
    {
        float value = 0F;
        for (int frame = 0; frame < 120; frame++)
        {
            value = UIMotion.Approach(value, 1F, 16F, 1F / 60F);
        }

        Assert.Equal(1F, value);
        Assert.InRange(UIMotion.Approach(0F, 1F, 16F, 1F / 60F), 0.1F, 0.5F);
    }

    [Theory]
    [InlineData(-1F, 0F)]
    [InlineData(0.05F, 0.5F)]
    [InlineData(1F, 1F)]
    [InlineData(2.6F, 0.5F)]
    [InlineData(5F, 0F)]
    public void EnvelopeFadesInHoldsAndFadesOut(float elapsed, float expected)
    {
        Assert.Equal(expected, UIMotion.Envelope(elapsed, 0.1F, 2F, 1F), 3);
    }

    [Fact]
    public void FocusStepWrapsAndSkipsUnfocusable()
    {
        bool[] focusable = [true, false, true, true];

        Assert.Equal(0, UIFocus.Step(-1, 4, 1, index => focusable[index]));
        Assert.Equal(2, UIFocus.Step(0, 4, 1, index => focusable[index]));
        Assert.Equal(0, UIFocus.Step(3, 4, 1, index => focusable[index]));
        Assert.Equal(3, UIFocus.Step(0, 4, -1, index => focusable[index]));
        Assert.Equal(3, UIFocus.Step(-1, 4, -1, index => focusable[index]));
    }

    [Fact]
    public void FocusStepFindsNothingWhenNothingCanFocus()
    {
        Assert.Equal(-1, UIFocus.Step(0, 3, 1, _ => false));
        Assert.Equal(-1, UIFocus.Step(0, 0, 1, _ => true));
    }

    [Theory]
    [InlineData(0, 3, 1, 1)]
    [InlineData(2, 3, 1, 0)]
    [InlineData(0, 3, -1, 2)]
    public void CycleWrapsAround(int index, int count, int step, int expected)
    {
        Assert.Equal(expected, UICycleButton.Cycle(index, count, step));
    }

    [Theory]
    [InlineData(3.4F, 2F, 16F, 1F, 3F)]
    [InlineData(99F, 2F, 16F, 1F, 16F)]
    [InlineData(0.333F, 0F, 1F, 0.01F, 0.33F)]
    [InlineData(0.7F, 0F, 1F, 0F, 0.7F)]
    public void SliderSnapsToItsStep(float value, float minimum, float maximum, float step, float expected)
    {
        Assert.Equal(expected, UISlider.Snap(value, minimum, maximum, step), 4);
    }

    [Fact]
    public void SliderMapsPixelsAcrossItsTravel()
    {
        Assert.Equal(0F, UISlider.ValueAt(0F, 0F, 110F, 0F, 1F));
        Assert.Equal(1F, UISlider.ValueAt(200F, 0F, 110F, 0F, 1F));
        Assert.Equal(0.5F, UISlider.ValueAt(55F, 0F, 110F, 0F, 1F), 3);
    }

    [Theory]
    [InlineData(0, 0, 5, 20, 0)]
    [InlineData(0, 7, 5, 20, 3)]
    [InlineData(10, 4, 5, 20, 4)]
    [InlineData(50, -1, 5, 20, 15)]
    [InlineData(3, 2, 5, 3, 0)]
    public void ListScrollKeepsTheSelectionVisible(int scroll, int selected, int visible, int count, int expected)
    {
        Assert.Equal(expected, UIList.ClampScroll(scroll, selected, visible, count));
    }

    [Fact]
    public void ListHeightCountsRowsGapsAndInsets()
    {
        Assert.Equal((2 * UIList.Inset) + (3 * UIList.RowHeight) + (2 * UIList.RowGap), UIList.HeightFor(3));
    }

    [Fact]
    public void TooltipSitsBelowRightOfTheCursorWhenThereIsRoom()
    {
        Vector2 position = UITooltip.Place(new Vector2(100F, 100F), new Vector2(50F, 20F), new Vector2(800F, 600F), 10F, 4F);

        Assert.Equal(new Vector2(110F, 110F), position);
    }

    [Fact]
    public void TooltipFlipsAwayFromTheEdges()
    {
        Vector2 position = UITooltip.Place(new Vector2(790F, 590F), new Vector2(50F, 20F), new Vector2(800F, 600F), 10F, 4F);

        Assert.Equal(new Vector2(730F, 560F), position);
    }

    [Fact]
    public void SlotsKeepTheirSizeWhenEverythingFits()
    {
        Assert.Equal(46F, UICanvasInventory.FitSlotSize(46F, 26F, 600F, 10F, 700F));
    }

    [Fact]
    public void SlotsShrinkToFitAShortWindowButNotBelowTheMinimum()
    {
        float fitted = UICanvasInventory.FitSlotSize(46F, 26F, 800F, 12F, 600F);

        Assert.InRange(fitted, 26F, 45F);
        Assert.Equal(26F, UICanvasInventory.FitSlotSize(46F, 26F, 5000F, 12F, 300F));
    }

    [Theory]
    [InlineData(100, 2)]
    [InlineData(150, 5)]
    [InlineData(10, 0)]
    [InlineData(120, 3)]
    public void GuiScaleSnapsToTheNearestChoice(int percent, int expected)
    {
        Assert.Equal(expected, UICanvasOptions.NearestGuiScaleIndex(percent));
    }

    [Theory]
    [InlineData(0.0, 2)]
    [InlineData(0.6, 1)]
    [InlineData(1.5, 0)]
    [InlineData(9.0, 0)]
    public void DeleteIsOnlyArmedAfterAPause(double elapsed, int expected)
    {
        Assert.Equal(expected, UICanvasDeleteWorld.SecondsUntilArmed(elapsed));
    }

    [Fact]
    public void HeartIconsAreOutlinedAndTheHalfOnlyFillsTheLeft()
    {
        byte[] full = UIPixelArt.Heart(leftHalfOnly: false);
        byte[] half = UIPixelArt.Heart(leftHalfOnly: true);

        Assert.Equal(UIPixelArt.IconSize * UIPixelArt.IconSize * 4, full.Length);
        Assert.Equal(255, full[(((3 * UIPixelArt.IconSize) + 7) * 4) + 3]);
        Assert.Equal(0, half[(((3 * UIPixelArt.IconSize) + 7) * 4) + 3]);
        Assert.Equal(255, half[(((3 * UIPixelArt.IconSize) + 2) * 4) + 3]);
    }

    [Fact]
    public void TheMenuSceneIsOpaqueAndDeterministic()
    {
        byte[] first = UIPixelArt.Scene(64, 36, 7);
        byte[] second = UIPixelArt.Scene(64, 36, 7);

        Assert.Equal(first, second);
        for (int pixel = 3; pixel < first.Length; pixel += 4)
        {
            Assert.Equal(255, first[pixel]);
        }
    }
}

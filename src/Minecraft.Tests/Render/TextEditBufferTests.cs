using Minecraft.Core.Render.UI;

namespace Minecraft.Tests.Render;

public sealed class TextEditBufferTests
{
    private static TextEditBuffer With(string text, int maxLength = 64)
    {
        var buffer = new TextEditBuffer(maxLength);
        buffer.SetText(text);
        return buffer;
    }

    [Fact]
    public void SettingTextPutsTheCaretAtTheEnd()
    {
        TextEditBuffer buffer = With("hello");

        Assert.Equal(5, buffer.Caret);
        Assert.False(buffer.HasSelection);
    }

    [Fact]
    public void SettingTextTruncatesToTheMaximumLength()
    {
        TextEditBuffer buffer = With("abcdefgh", maxLength: 4);

        Assert.Equal("abcd", buffer.Text);
    }

    [Fact]
    public void InsertingAtTheCaretKeepsTheRest()
    {
        TextEditBuffer buffer = With("held");
        buffer.MoveTo(2, extend: false);

        buffer.Insert("XY");

        Assert.Equal("heXYld", buffer.Text);
        Assert.Equal(4, buffer.Caret);
    }

    [Fact]
    public void InsertingReplacesTheSelection()
    {
        TextEditBuffer buffer = With("hello world");
        buffer.MoveTo(6, extend: false);
        buffer.MoveEnd(extend: true);

        buffer.Insert("there");

        Assert.Equal("hello there", buffer.Text);
        Assert.False(buffer.HasSelection);
    }

    [Fact]
    public void InsertingRespectsTheMaximumLength()
    {
        TextEditBuffer buffer = With("abc", maxLength: 5);

        buffer.Insert("defgh");

        Assert.Equal("abcde", buffer.Text);
    }

    [Fact]
    public void InsertingIntoAFullSelectionUsesTheFreedRoom()
    {
        TextEditBuffer buffer = With("abcde", maxLength: 5);
        buffer.SelectAll();

        buffer.Insert("xyz");

        Assert.Equal("xyz", buffer.Text);
    }

    [Fact]
    public void ControlCharactersAreDroppedAndLineBreaksBecomeSpaces()
    {
        TextEditBuffer buffer = With(string.Empty);

        buffer.Insert("a\u0001b\nc");

        Assert.Equal("ab c", buffer.Text);
    }

    [Fact]
    public void BackspaceRemovesOneCharacter()
    {
        TextEditBuffer buffer = With("abc");

        buffer.Backspace(word: false);

        Assert.Equal("ab", buffer.Text);
    }

    [Fact]
    public void WordBackspaceRemovesTheWordAndTheSpacesBeforeTheCaret()
    {
        TextEditBuffer buffer = With("hello big world  ");

        buffer.Backspace(word: true);

        Assert.Equal("hello big ", buffer.Text);
    }

    [Fact]
    public void WordDeleteRemovesTheWordAndTheSpacesAfterIt()
    {
        TextEditBuffer buffer = With("one two three");
        buffer.MoveTo(4, extend: false);

        buffer.Delete(word: true);

        Assert.Equal("one three", buffer.Text);
    }

    [Fact]
    public void BackspaceWithASelectionOnlyRemovesTheSelection()
    {
        TextEditBuffer buffer = With("abcdef");
        buffer.MoveTo(1, extend: false);
        buffer.MoveTo(3, extend: true);

        buffer.Backspace(word: true);

        Assert.Equal("adef", buffer.Text);
        Assert.Equal(1, buffer.Caret);
    }

    [Fact]
    public void MovingLeftWithoutShiftCollapsesTheSelectionToItsStart()
    {
        TextEditBuffer buffer = With("abcdef");
        buffer.MoveTo(2, extend: false);
        buffer.MoveTo(5, extend: true);

        buffer.MoveLeft(word: false, extend: false);

        Assert.Equal(2, buffer.Caret);
        Assert.False(buffer.HasSelection);
    }

    [Fact]
    public void ShiftArrowsGrowTheSelection()
    {
        TextEditBuffer buffer = With("abcdef");
        buffer.MoveHome(extend: false);

        buffer.MoveRight(word: false, extend: true);
        buffer.MoveRight(word: false, extend: true);

        Assert.Equal("ab", buffer.SelectedText);
    }

    [Fact]
    public void CutReturnsAndRemovesTheSelection()
    {
        TextEditBuffer buffer = With("copy me");
        buffer.SelectWordAt(1);

        string cut = buffer.Cut();

        Assert.Equal("copy", cut);
        Assert.Equal(" me", buffer.Text);
    }

    [Theory]
    [InlineData("one two", 7, 4)]
    [InlineData("one two", 4, 0)]
    [InlineData("one   two", 6, 0)]
    [InlineData("", 0, 0)]
    public void PreviousWordBoundarySkipsSpacesThenTheWord(string text, int from, int expected)
    {
        Assert.Equal(expected, TextEditBuffer.PreviousWordBoundary(text, from));
    }

    [Theory]
    [InlineData("one two", 0, 4)]
    [InlineData("one two", 4, 7)]
    [InlineData("one", 3, 3)]
    public void NextWordBoundarySkipsTheWordThenSpaces(string text, int from, int expected)
    {
        Assert.Equal(expected, TextEditBuffer.NextWordBoundary(text, from));
    }

    [Fact]
    public void EveryEditBumpsTheVersion()
    {
        TextEditBuffer buffer = With("a");
        int before = buffer.Version;

        buffer.Insert("b");

        Assert.NotEqual(before, buffer.Version);
    }
}

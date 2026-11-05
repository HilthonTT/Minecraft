using System.Text;

namespace Minecraft.Core.Render.UI;

public sealed class TextEditBuffer
{
    private readonly int _maxLength;

    public TextEditBuffer(int maxLength)
    {
        _maxLength = Math.Max(0, maxLength);
    }

    public string Text { get; private set; } = string.Empty;

    public int Caret { get; private set; }

    public int Anchor { get; private set; }

    public int Version { get; private set; }

    public int MaxLength => _maxLength;

    public bool HasSelection => Caret != Anchor;

    public int SelectionStart => Math.Min(Caret, Anchor);

    public int SelectionEnd => Math.Max(Caret, Anchor);

    public string SelectedText => Text[SelectionStart..SelectionEnd];

    public void SetText(string text)
    {
        string cleaned = Clean(text);
        Text = cleaned.Length > _maxLength ? cleaned[.._maxLength] : cleaned;
        Caret = Text.Length;
        Anchor = Caret;
        Version++;
    }

    public void Insert(string text)
    {
        string cleaned = Clean(text);
        if (cleaned.Length == 0)
        {
            return;
        }

        string remaining = Text.Remove(SelectionStart, SelectionEnd - SelectionStart);
        int room = _maxLength - remaining.Length;
        if (room <= 0)
        {
            return;
        }

        if (cleaned.Length > room)
        {
            cleaned = cleaned[..room];
        }

        int at = SelectionStart;
        Text = remaining.Insert(at, cleaned);
        Caret = at + cleaned.Length;
        Anchor = Caret;
        Version++;
    }

    public void Backspace(bool word)
    {
        if (DeleteSelection())
        {
            return;
        }

        if (Caret == 0)
        {
            return;
        }

        int from = word ? PreviousWordBoundary(Text, Caret) : Caret - 1;
        RemoveRange(from, Caret);
    }

    public void Delete(bool word)
    {
        if (DeleteSelection())
        {
            return;
        }

        if (Caret >= Text.Length)
        {
            return;
        }

        int to = word ? NextWordBoundary(Text, Caret) : Caret + 1;
        RemoveRange(Caret, to);
    }

    public string Cut()
    {
        string selected = SelectedText;
        DeleteSelection();
        return selected;
    }

    public void MoveLeft(bool word, bool extend)
    {
        if (HasSelection && !extend)
        {
            MoveTo(SelectionStart, extend: false);
            return;
        }

        MoveTo(word ? PreviousWordBoundary(Text, Caret) : Math.Max(0, Caret - 1), extend);
    }

    public void MoveRight(bool word, bool extend)
    {
        if (HasSelection && !extend)
        {
            MoveTo(SelectionEnd, extend: false);
            return;
        }

        MoveTo(word ? NextWordBoundary(Text, Caret) : Math.Min(Text.Length, Caret + 1), extend);
    }

    public void MoveHome(bool extend) => MoveTo(0, extend);

    public void MoveEnd(bool extend) => MoveTo(Text.Length, extend);

    public void SelectAll()
    {
        Anchor = 0;
        Caret = Text.Length;
        Version++;
    }

    public void MoveTo(int index, bool extend)
    {
        Caret = Math.Clamp(index, 0, Text.Length);
        if (!extend)
        {
            Anchor = Caret;
        }

        Version++;
    }

    public void SelectWordAt(int index)
    {
        int at = Math.Clamp(index, 0, Text.Length);
        int start = at;
        int end = at;

        while (start > 0 && !char.IsWhiteSpace(Text[start - 1]))
        {
            start--;
        }

        while (end < Text.Length && !char.IsWhiteSpace(Text[end]))
        {
            end++;
        }

        Anchor = start;
        Caret = end;
        Version++;
    }

    public static int PreviousWordBoundary(string text, int from)
    {
        int index = Math.Clamp(from, 0, text.Length);

        while (index > 0 && char.IsWhiteSpace(text[index - 1]))
        {
            index--;
        }

        while (index > 0 && !char.IsWhiteSpace(text[index - 1]))
        {
            index--;
        }

        return index;
    }

    public static int NextWordBoundary(string text, int from)
    {
        int index = Math.Clamp(from, 0, text.Length);

        while (index < text.Length && !char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        return index;
    }

    private bool DeleteSelection()
    {
        if (!HasSelection)
        {
            return false;
        }

        RemoveRange(SelectionStart, SelectionEnd);
        return true;
    }

    private void RemoveRange(int from, int to)
    {
        Text = Text.Remove(from, to - from);
        Caret = from;
        Anchor = from;
        Version++;
    }

    private static string Clean(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char character in text)
        {
            if (character is '\n' or '\r' or '\t')
            {
                builder.Append(' ');
            }
            else if (!char.IsControl(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}

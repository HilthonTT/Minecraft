using System.Text;

namespace Minecraft.Core.Render.UI;

public static class UITextLayout
{
    public const string Ellipsis = "...";

    public static string Ellipsize(string text, float maxWidth, Func<string, float> measure)
    {
        if (maxWidth <= 0 || measure(text) <= maxWidth)
        {
            return text;
        }

        float ellipsisWidth = measure(Ellipsis);

        int fitting = text.Length;
        while (fitting > 0 && measure(text[..fitting]) + ellipsisWidth > maxWidth)
        {
            fitting--;
        }

        return fitting == 0 ? Ellipsis : text[..fitting].TrimEnd() + Ellipsis;
    }

    public static List<string> Wrap(string text, float maxWidth, Func<string, float> measure)
    {
        var lines = new List<string>();

        foreach (string paragraph in text.Split('\n'))
        {
            WrapParagraph(paragraph, maxWidth, measure, lines);
        }

        return lines;
    }

    public static string WrapToString(string text, float maxWidth, Func<string, float> measure)
    {
        return string.Join('\n', Wrap(text, maxWidth, measure));
    }

    public static int IndexAtOffset(string text, float offset, Func<string, float> measure)
    {
        if (offset <= 0)
        {
            return 0;
        }

        float previous = 0;
        for (int index = 1; index <= text.Length; index++)
        {
            float width = measure(text[..index]);
            if (offset < width)
            {
                return offset - previous < width - offset ? index - 1 : index;
            }

            previous = width;
        }

        return text.Length;
    }

    public static int FirstVisibleIndex(string text, int caret, int currentFirst, float width, Func<string, float> measure)
    {
        int first = Math.Clamp(currentFirst, 0, Math.Clamp(caret, 0, text.Length));

        while (first < caret && measure(text[first..caret]) > width)
        {
            first++;
        }

        while (first > 0 && measure(text[(first - 1)..]) <= width)
        {
            first--;
        }

        return first;
    }

    private static void WrapParagraph(string paragraph, float maxWidth, Func<string, float> measure, List<string> lines)
    {
        if (paragraph.Length == 0 || maxWidth <= 0 || measure(paragraph) <= maxWidth)
        {
            lines.Add(paragraph);
            return;
        }

        var line = new StringBuilder();

        foreach (string word in paragraph.Split(' '))
        {
            string candidate = line.Length == 0 ? word : line + " " + word;
            if (measure(candidate) <= maxWidth)
            {
                line.Clear().Append(candidate);
                continue;
            }

            if (line.Length > 0)
            {
                lines.Add(line.ToString());
                line.Clear();
            }

            string rest = word;
            while (rest.Length > 0 && measure(rest) > maxWidth)
            {
                int fitting = 1;
                while (fitting < rest.Length && measure(rest[..(fitting + 1)]) <= maxWidth)
                {
                    fitting++;
                }

                lines.Add(rest[..fitting]);
                rest = rest[fitting..];
            }

            line.Append(rest);
        }

        if (line.Length > 0)
        {
            lines.Add(line.ToString());
        }
    }
}

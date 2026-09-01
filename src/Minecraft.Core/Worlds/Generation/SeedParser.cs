using System.Globalization;

namespace Minecraft.Core.Worlds.Generation;

public static class SeedParser
{
    public static int? Parse(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
        {
            return number;
        }

        return Hash(trimmed);
    }

    private static int Hash(string text)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        uint hash = offsetBasis;
        foreach (char character in text)
        {
            hash = (hash ^ character) * prime;
        }

        return unchecked((int)hash);
    }
}

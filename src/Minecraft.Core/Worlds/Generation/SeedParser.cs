using System.Globalization;

namespace Minecraft.Core.Worlds.Generation;

/// <summary>
/// Turns what somebody typed into a seed. A number is taken as it stands, so a seed can be written down and
/// used again, and anything else is hashed, so a world can be asked for by word and still come back the same.
/// </summary>
public static class SeedParser
{
    /// <summary>
    /// The seed the given text asks for, or null when it is blank, which is how a world is asked to pick one
    /// of its own.
    /// </summary>
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

    /// <summary>
    /// FNV-1a rather than the built in string hash, which is salted per process and would hand the same word
    /// a different world every time the game was started.
    /// </summary>
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

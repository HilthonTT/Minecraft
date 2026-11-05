using System.Globalization;
using Minecraft.Core.Games;
using Minecraft.Core.Worlds.Storage;

namespace Minecraft.Core.Render.UI.Presets;

public sealed record WorldSummary(string Name, GameMode? GameMode, int? Seed, DateTime? LastPlayed)
{
    public static WorldSummary Read(string savesRoot, string name)
    {
        string directory = WorldSaves.GetWorldDirectory(savesRoot, name);
        string path = Path.Combine(directory, WorldMetadataFile.FileName);

        try
        {
            DateTime lastPlayed = File.GetLastWriteTime(path);
            if (Directory.Exists(directory))
            {
                DateTime touched = Directory.GetLastWriteTime(directory);
                lastPlayed = touched > lastPlayed ? touched : lastPlayed;
            }

            return Parse(name, File.ReadAllLines(path), lastPlayed);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new WorldSummary(name, null, null, null);
        }
    }

    public static WorldSummary Parse(string name, IEnumerable<string> lines, DateTime? lastPlayed)
    {
        GameMode? gameMode = null;
        int? seed = null;

        foreach (string line in lines)
        {
            string[] keyValue = line.Trim().Split('=', 2);
            if (keyValue.Length != 2)
            {
                continue;
            }

            string key = keyValue[0].Trim().ToLowerInvariant();
            string value = keyValue[1].Trim();

            if (key == "seed" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSeed))
            {
                seed = parsedSeed;
            }
            else if (key == "gamemode" && Enum.TryParse(value, ignoreCase: true, out GameMode parsedMode) &&
                     Enum.IsDefined(parsedMode))
            {
                gameMode = parsedMode;
            }
        }

        return new WorldSummary(name, gameMode, seed, lastPlayed);
    }

    public string Describe(DateTime now)
    {
        var parts = new List<string>();

        if (GameMode is GameMode mode)
        {
            parts.Add(mode == Games.GameMode.Creative ? "Creative" : "Survival");
        }

        if (LastPlayed is DateTime played)
        {
            parts.Add("Played " + DescribeAge(played, now));
        }

        if (Seed is int seed)
        {
            parts.Add("Seed " + seed.ToString(CultureInfo.InvariantCulture));
        }

        return parts.Count == 0 ? "No details saved" : string.Join("   |   ", parts);
    }

    public static string DescribeAge(DateTime then, DateTime now)
    {
        TimeSpan age = now - then;

        if (age < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (age < TimeSpan.FromHours(1))
        {
            return Plural((int)age.TotalMinutes, "minute") + " ago";
        }

        if (age < TimeSpan.FromDays(1) && then.Date == now.Date)
        {
            return Plural((int)age.TotalHours, "hour") + " ago";
        }

        if (then.Date == now.Date.AddDays(-1))
        {
            return "yesterday";
        }

        if (age < TimeSpan.FromDays(30))
        {
            return Plural(Math.Max(2, (int)Math.Ceiling((now.Date - then.Date).TotalDays)), "day") + " ago";
        }

        return "on " + then.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
    }

    private static string Plural(int count, string unit)
    {
        return count.ToString(CultureInfo.InvariantCulture) + " " + unit + (count == 1 ? string.Empty : "s");
    }
}

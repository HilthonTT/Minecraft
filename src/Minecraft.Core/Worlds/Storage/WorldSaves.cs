using System.Text;
using Minecraft.Core.Logging;

namespace Minecraft.Core.Worlds.Storage;

public static class WorldSaves
{
    private const string FallbackWorldName = "world";

    private const int MaxSuggestedSuffix = 1000;

    public static string GetWorldDirectory(string savesRoot, string worldName)
    {
        return Path.Combine(savesRoot, SanitiseWorldName(worldName));
    }

    public static bool WorldExists(string savesRoot, string worldName)
    {
        return File.Exists(Path.Combine(GetWorldDirectory(savesRoot, worldName), WorldMetadataFile.FileName));
    }

    public static string SuggestUnusedWorldName(string savesRoot, string preferred)
    {
        if (!WorldExists(savesRoot, preferred))
        {
            return preferred;
        }

        for (int suffix = 2; suffix < MaxSuggestedSuffix; suffix++)
        {
            string candidate = preferred + suffix;
            if (!WorldExists(savesRoot, candidate))
            {
                return candidate;
            }
        }

        return preferred;
    }

    public static List<string> ListWorlds(string savesRoot)
    {
        if (!Directory.Exists(savesRoot))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateDirectories(savesRoot)
                .Where(directory => File.Exists(Path.Combine(directory, WorldMetadataFile.FileName)))
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .Select(Path.GetFileName)
                .OfType<string>()
                .ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Error("Could not read the saves directory at " + savesRoot + ": " + e.Message);
            return [];
        }
    }

    public static WorldRenameResult TryRenameWorld(string savesRoot, string fromName, string toName)
    {
        string source = GetWorldDirectory(savesRoot, fromName);
        string destination = GetWorldDirectory(savesRoot, toName);

        if (!Directory.Exists(source))
        {
            return WorldRenameResult.SourceMissing;
        }

        if (source == destination)
        {
            return WorldRenameResult.Unchanged;
        }

        bool differsOnlyByCase = string.Equals(source, destination, StringComparison.OrdinalIgnoreCase);
        if (!differsOnlyByCase && Directory.Exists(destination))
        {
            return WorldRenameResult.NameTaken;
        }

        try
        {
            if (differsOnlyByCase)
            {
                string staging = destination + ".renaming";
                Directory.Move(source, staging);
                Directory.Move(staging, destination);
            }
            else
            {
                Directory.Move(source, destination);
            }

            Logger.Info("Renamed world '" + Path.GetFileName(source) + "' to '" + Path.GetFileName(destination) + "'.");
            return WorldRenameResult.Renamed;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Error("Could not rename the world at " + source + ": " + e.Message);
            return WorldRenameResult.Failed;
        }
    }

    public static bool TryDeleteWorld(string savesRoot, string worldName)
    {
        string directory = GetWorldDirectory(savesRoot, worldName);
        if (!Directory.Exists(directory))
        {
            return false;
        }

        try
        {
            Directory.Delete(directory, recursive: true);
            Logger.Info("Deleted the world at " + directory + ".");
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Error("Could not delete the world at " + directory + ": " + e.Message);
            return false;
        }
    }

    public static string SanitiseWorldName(string worldName)
    {
        string trimmed = worldName.Trim();
        if (trimmed.Length == 0)
        {
            return FallbackWorldName;
        }

        char[] invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(trimmed.Length);
        foreach (char character in trimmed)
        {
            builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
        }

        string sanitised = builder.ToString().Trim('.', ' ');
        return sanitised.Length == 0 ? FallbackWorldName : sanitised;
    }
}

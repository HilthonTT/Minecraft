using System.Globalization;
using Minecraft.Core.Games;
using Minecraft.Core.IO;
using Minecraft.Core.Logging;

namespace Minecraft.Core.Worlds.Storage;

public static class WorldMetadataFile
{
    public const string FileName = "level.dat";

    public static WorldMetadata LoadOrCreate(string worldDirectory, int? seed, GameMode? gameMode)
    {
        string path = Path.Combine(worldDirectory, FileName);
        string worldName = Path.GetFileName(worldDirectory);

        if (!File.Exists(path))
        {
            return CreateNew(worldName, seed, gameMode);
        }

        try
        {
            Dictionary<string, string> fields = ReadKeyValueFile(path);

            int version = ReadInt(fields, "version", WorldMetadata.CurrentVersion);
            if (version != WorldMetadata.CurrentVersion)
            {
                throw new InvalidDataException(
                    $"Save format version {version} cannot be read by this build, which expects " +
                    $"{WorldMetadata.CurrentVersion}.");
            }

            var metadata = new WorldMetadata
            {
                Version = version,
                Seed = ReadInt(fields, "seed", 0),
                CurrentTime = ReadFloat(fields, "time", World.MiddayTimeSeconds),

                GameMode = ReadGameMode(fields, "gamemode", GameMode.Creative),
            };

            WarnAboutIgnoredSettings(worldName, metadata, seed, gameMode);

            Logger.Info("Loaded world '" + worldName + "' with seed " + metadata.Seed + ".");
            return metadata;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or FormatException)
        {
            throw new InvalidDataException("Failed to read " + path + ": " + e.Message, e);
        }
    }

    public static void Save(string worldDirectory, WorldMetadata metadata)
    {
        try
        {
            Directory.CreateDirectory(worldDirectory);

            string path = Path.Combine(worldDirectory, FileName);
            string[] lines =
            [
                "version=" + metadata.Version.ToString(CultureInfo.InvariantCulture),
                "seed=" + metadata.Seed.ToString(CultureInfo.InvariantCulture),
                "time=" + metadata.CurrentTime.ToString("0.###", CultureInfo.InvariantCulture),
                "gamemode=" + metadata.GameMode.ToString().ToLowerInvariant(),
            ];

            AtomicFile.Write(path, stream =>
            {
                using var writer = new StreamWriter(stream);
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            });
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Error("Failed to save world metadata: " + e.Message);
        }
    }

    private static WorldMetadata CreateNew(string worldName, int? seed, GameMode? gameMode)
    {
        int newSeed = seed ?? Random.Shared.Next();
        GameMode newGameMode = gameMode ?? GameMode.Survival;

        Logger.Info(
            "Creating world '" + worldName + "' with seed " + newSeed +
            " in " + newGameMode.ToString().ToLowerInvariant() + " mode.");

        return new WorldMetadata { Seed = newSeed, GameMode = newGameMode };
    }

    private static void WarnAboutIgnoredSettings(
        string worldName,
        WorldMetadata metadata,
        int? seed,
        GameMode? gameMode)
    {
        if (seed.HasValue && seed.Value != metadata.Seed)
        {
            Logger.Warn(
                "Ignoring seed " + seed.Value + ": world '" + worldName +
                "' already exists with seed " + metadata.Seed + ".");
        }

        if (gameMode.HasValue && gameMode.Value != metadata.GameMode)
        {
            Logger.Warn(
                "Ignoring game mode " + gameMode.Value + ": world '" + worldName +
                "' already exists in " + metadata.GameMode + " mode.");
        }
    }

    private static Dictionary<string, string> ReadKeyValueFile(string path)
    {
        Dictionary<string, string> fields = [];

        foreach (string line in File.ReadAllLines(path))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            string[] keyValue = trimmed.Split('=', 2);
            if (keyValue.Length == 2)
            {
                fields[keyValue[0].Trim().ToLowerInvariant()] = keyValue[1].Trim();
            }
        }

        return fields;
    }

    private static int ReadInt(Dictionary<string, string> fields, string key, int fallback)
    {
        if (!fields.TryGetValue(key, out string? value))
        {
            return fallback;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : throw new FormatException($"'{key}' is not a whole number: '{value}'.");
    }

    private static GameMode ReadGameMode(Dictionary<string, string> fields, string key, GameMode fallback)
    {
        if (!fields.TryGetValue(key, out string? value))
        {
            return fallback;
        }

        return Enum.TryParse(value, ignoreCase: true, out GameMode parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new FormatException($"'{key}' is not a game mode: '{value}'.");
    }

    private static float ReadFloat(Dictionary<string, string> fields, string key, float fallback)
    {
        if (!fields.TryGetValue(key, out string? value))
        {
            return fallback;
        }

        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : throw new FormatException($"'{key}' is not a number: '{value}'.");
    }
}

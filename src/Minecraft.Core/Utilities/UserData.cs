namespace Minecraft.Core.Utilities;

public static class UserData
{
    public const string FolderName = "MinecraftClone";
    public const string OverrideVariable = "MINECRAFT_CLONE_DATA";
    public const string PortableMarker = "portable.txt";

    private static readonly string[] _legacyEntries = ["saves", "options.txt", PortableMarker];

    private static readonly Lazy<string> _root = new(() => ResolveRoot(
        AppContext.BaseDirectory,
        Environment.GetEnvironmentVariable(OverrideVariable),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));

    public static string Root => _root.Value;

    public static string Path(string relativePath)
    {
        return System.IO.Path.Combine(Root, relativePath);
    }

    public static string ResolveRoot(string baseDirectory, string? overrideDirectory, string? applicationDataDirectory)
    {
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return EnsureExists(System.IO.Path.GetFullPath(overrideDirectory));
        }

        foreach (string entry in _legacyEntries)
        {
            string legacyPath = System.IO.Path.Combine(baseDirectory, entry);
            if (Directory.Exists(legacyPath) || File.Exists(legacyPath))
            {
                return baseDirectory;
            }
        }

        if (string.IsNullOrWhiteSpace(applicationDataDirectory))
        {
            return baseDirectory;
        }

        return EnsureExists(System.IO.Path.Combine(applicationDataDirectory, FolderName));
    }

    private static string EnsureExists(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return AppContext.BaseDirectory;
        }

        return directory;
    }
}

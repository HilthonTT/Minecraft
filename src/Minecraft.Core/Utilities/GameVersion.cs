using System.Reflection;

namespace Minecraft.Core.Utilities;

public static class GameVersion
{
    public static string Current { get; } = Read();

    public static string DisplayName => "Minecraft Clone " + Current;

    private static string Read()
    {
        string? informational = typeof(GameVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informational))
        {
            return "dev";
        }

        int metadata = informational.IndexOf('+');
        return metadata >= 0 ? informational[..metadata] : informational;
    }
}

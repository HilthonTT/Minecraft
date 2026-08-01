namespace Minecraft.Core.Utilities;

/// <summary>
/// Resolves paths to the shader and resource files that are copied next to the assembly. Resolving against
/// the assembly location rather than the working directory keeps loading independent of where the game was
/// launched from.
/// </summary>
public static class Assets
{
    /// <summary>Turns a path relative to the output directory into an absolute one.</summary>
    public static string Path(string relativePath)
    {
        return System.IO.Path.Combine(AppContext.BaseDirectory, relativePath);
    }
}

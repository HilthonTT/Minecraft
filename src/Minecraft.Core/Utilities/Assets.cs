namespace Minecraft.Core.Utilities;

public static class Assets
{
    public static string Path(string relativePath)
    {
        return System.IO.Path.Combine(AppContext.BaseDirectory, relativePath);
    }
}

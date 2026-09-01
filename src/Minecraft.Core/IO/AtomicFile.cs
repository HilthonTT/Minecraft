namespace Minecraft.Core.IO;

public static class AtomicFile
{
    private const string TemporaryExtension = ".tmp";

    public static void Write(string path, Action<Stream> write)
    {
        string temporaryPath = path + TemporaryExtension;

        using (FileStream file = File.Create(temporaryPath))
        {
            write(file);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }
}

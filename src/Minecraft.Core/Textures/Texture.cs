using Minecraft.Core.Utilities;

namespace Minecraft.Core.Textures;

public class Texture
{
    public int Id { get; set; }

    public int PixelWidth { get; set; }

    public int PixelHeight { get; set; }

    public Texture()
    {
    }

    public Texture(int textureId, int pixelWidth, int pixelHeight)
    {
        Id = textureId;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }

    public Texture(string pathToFile, int pixelWidth, int pixelHeight)
    {
        Id = TextureLoader.LoadTexture(pathToFile);
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }
}

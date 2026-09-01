using Minecraft.Core.Textures;

namespace Minecraft.Core.Render.UI;

public static class UITextures
{
    private static Texture? _white;

    public static Texture White => _white ??= new Texture(TextureLoader.LoadSolidWhiteTexture(), 1, 1);
}

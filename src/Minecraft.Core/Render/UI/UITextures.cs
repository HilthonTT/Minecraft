using Minecraft.Core.Textures;

namespace Minecraft.Core.Render.UI;

/// <summary>Textures the UI builds for itself instead of loading them from disk.</summary>
public static class UITextures
{
    private static Texture? _white;

    /// <summary>
    /// One opaque white pixel, stretched over whatever quad uses it. Panels tint it through
    /// <see cref="UIComponent.Color"/> and <see cref="UIComponent.Transparency"/>.
    /// </summary>
    public static Texture White => _white ??= new Texture(TextureLoader.LoadSolidWhiteTexture(), 1, 1);
}

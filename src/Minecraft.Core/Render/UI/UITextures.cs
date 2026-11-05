using Minecraft.Core.Textures;
using OpenTK.Graphics.OpenGL;

namespace Minecraft.Core.Render.UI;

public static class UITextures
{
    private const int SceneSeed = 1337;

    private static Texture? _white;
    private static Texture? _gradient;
    private static Texture? _vignette;
    private static Texture? _heart;
    private static Texture? _halfHeart;
    private static Texture? _shank;
    private static Texture? _halfShank;
    private static Texture? _scene;

    public static Texture White => _white ??= new Texture(TextureLoader.LoadSolidWhiteTexture(), 1, 1);

    public static Texture Gradient => _gradient ??= Create(1, 32, UIPixelArt.VerticalGradient(32, 1.0F, 0.74F), true);

    public static Texture Vignette => _vignette ??= Create(64, 64, UIPixelArt.Vignette(64, 0.35F, 0.85F), true);

    public static Texture Heart => _heart ??= CreateIcon(UIPixelArt.Heart(leftHalfOnly: false));

    public static Texture HalfHeart => _halfHeart ??= CreateIcon(UIPixelArt.Heart(leftHalfOnly: true));

    public static Texture Shank => _shank ??= CreateIcon(UIPixelArt.Shank(leftHalfOnly: false));

    public static Texture HalfShank => _halfShank ??= CreateIcon(UIPixelArt.Shank(leftHalfOnly: true));

    public static Texture Scene => _scene ??= Create(
        UIPixelArt.SceneWidth,
        UIPixelArt.SceneHeight,
        UIPixelArt.Scene(UIPixelArt.SceneWidth, UIPixelArt.SceneHeight, SceneSeed),
        false);

    private static Texture CreateIcon(byte[] pixels) => Create(UIPixelArt.IconSize, UIPixelArt.IconSize, pixels, false);

    private static Texture Create(int width, int height, byte[] rgba, bool smooth)
    {
        GL.GenTextures(1, out int texture);
        GL.BindTexture(TextureTarget.Texture2D, texture);

        var filter = smooth ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
        var magnify = smooth ? TextureMagFilter.Linear : TextureMagFilter.Nearest;

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magnify);

        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            width,
            height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            rgba);

        return new Texture(texture, width, height);
    }
}

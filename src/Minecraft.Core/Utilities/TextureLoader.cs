using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Minecraft.Core.Utilities;

[SupportedOSPlatform("windows")]
public static class TextureLoader
{
    private static readonly List<int> _textures = [];

    public static void Cleanup()
    {
        foreach (int texture in _textures)
        {
            GL.DeleteTexture(texture);
        }
        _textures.Clear();
    }

    public static int LoadTexture(string filePath)
    {
        GL.GenTextures(1, out int texture);
        GL.BindTexture(TextureTarget.Texture2D, texture);

        using var image = new Bitmap(filePath);
        var data = image.LockBits(
            new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            // Format32bppArgb is laid out BGRA in memory, so the upload has to be told that.
            GL.TexImage2D(
                TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
        }
        finally
        {
            image.UnlockBits(data);
        }

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        //GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, 1.0f);

        _textures.Add(texture);
        return texture;
    }

    public static int LoadDitherTexture()
    {
        // 8x8 Bayer ordered dithering pattern
        byte[] pattern = {
                0, 32,  8, 40,  2, 34, 10, 42,
                48, 16, 56, 24, 50, 18, 58, 26,
                12, 44,  4, 36, 14, 46,  6, 38,
                60, 28, 52, 20, 62, 30, 54, 22,
                3, 35, 11, 43,  1, 33,  9, 41,
                51, 19, 59, 27, 49, 17, 57, 25,
                15, 47,  7, 39, 13, 45,  5, 37,
                63, 31, 55, 23, 61, 29, 53, 21 };

        GL.GenTextures(1, out int texture);
        GL.BindTexture(TextureTarget.Texture2D, texture);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        IntPtr unmanagedPointer = Marshal.AllocHGlobal(pattern.Length);
        Marshal.Copy(pattern, 0, unmanagedPointer, pattern.Length);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Luminance, 8, 8, 0, OpenTK.Graphics.OpenGL.PixelFormat.Luminance, PixelType.UnsignedByte, unmanagedPointer);
        Marshal.FreeHGlobal(unmanagedPointer);

        _textures.Add(texture);
        return texture;
    }
}

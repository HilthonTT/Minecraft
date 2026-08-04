using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
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

    /// <param name="smooth">
    /// Interpolates between texels instead of snapping to the nearest one. Block textures want the hard
    /// pixels, but a font map that is drawn smaller than it was authored needs the filtering.
    /// </param>
    public static int LoadTexture(string filePath, bool smooth = false)
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

        int filter = smooth ? (int)TextureMinFilter.Linear : (int)TextureMinFilter.Nearest;
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, filter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, filter);
        //GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, 1.0f);

        _textures.Add(texture);
        return texture;
    }

    /// <summary>
    /// Loads the block sheet, turning the white background of its cut out cells into real transparency.
    /// <para>
    /// The sheet has no alpha channel of its own and marks the see through parts of a plant in white. Doing
    /// the conversion here, once, rather than testing for white while drawing means the rule applies to the
    /// cells that were drawn that way and to no others, so a block that is legitimately white keeps its
    /// colour. See <see cref="Shapes.BlockAtlas"/>.
    /// </para>
    /// </summary>
    /// <param name="cutOutCells">The cells whose white is a background rather than a colour.</param>
    /// <param name="cellsPerRow">How many cells the sheet is divided into along each edge.</param>
    public static int LoadBlockAtlas(string filePath, IReadOnlyList<Vector2> cutOutCells, int cellsPerRow)
    {
        GL.GenTextures(1, out int texture);
        GL.BindTexture(TextureTarget.Texture2D, texture);

        using var image = new Bitmap(filePath);

        // Locked as 32 bit even though the file has no alpha, which gives every texel an opaque alpha byte
        // for the cut out cells to punch through.
        var data = image.LockBits(
            new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            PunchOutWhiteBackgrounds(data, cutOutCells, cellsPerRow);

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

        _textures.Add(texture);
        return texture;
    }

    /// <summary>Clears the alpha of every white texel inside the given cells, leaving the rest untouched.</summary>
    private static void PunchOutWhiteBackgrounds(BitmapData data, IReadOnlyList<Vector2> cutOutCells, int cellsPerRow)
    {
        int cellWidth = data.Width / cellsPerRow;
        int cellHeight = data.Height / cellsPerRow;

        unsafe
        {
            var pixels = (byte*)data.Scan0;

            foreach (Vector2 cell in cutOutCells)
            {
                int startX = (int)cell.X * cellWidth;
                int startY = (int)cell.Y * cellHeight;

                for (int y = startY; y < startY + cellHeight; y++)
                {
                    byte* row = pixels + (y * data.Stride);

                    for (int x = startX; x < startX + cellWidth; x++)
                    {
                        byte* texel = row + (x * 4);

                        // Laid out blue, green, red, alpha.
                        if (texel[0] == 255 && texel[1] == 255 && texel[2] == 255)
                        {
                            texel[3] = 0;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// A single opaque white pixel. Drawn through the UI shader it takes on whatever colour and transparency
    /// the component carries, which is all a flat panel behind text needs.
    /// </summary>
    public static int LoadSolidWhiteTexture()
    {
        byte[] pixel = [255, 255, 255, 255];

        GL.GenTextures(1, out int texture);
        GL.BindTexture(TextureTarget.Texture2D, texture);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        IntPtr unmanagedPointer = Marshal.AllocHGlobal(pixel.Length);
        Marshal.Copy(pixel, 0, unmanagedPointer, pixel.Length);
        GL.TexImage2D(
            TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0,
            OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, unmanagedPointer);
        Marshal.FreeHGlobal(unmanagedPointer);

        _textures.Add(texture);
        return texture;
    }

    public static int LoadDitherTexture()
    {
        // 8x8 Bayer ordered dithering pattern
        byte[] pattern = 
        [
            0, 32,  8, 40,  2, 34, 10, 42,
            48, 16, 56, 24, 50, 18, 58, 26,
            12, 44,  4, 36, 14, 46,  6, 38,
            60, 28, 52, 20, 62, 30, 54, 22,
            3, 35, 11, 43,  1, 33,  9, 41,
            51, 19, 59, 27, 49, 17, 57, 25,
            15, 47,  7, 39, 13, 45,  5, 37,
            63, 31, 55, 23, 61, 29, 53, 21,
        ];

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

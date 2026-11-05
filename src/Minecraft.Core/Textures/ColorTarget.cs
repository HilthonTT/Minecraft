using Minecraft.Core.Logging;
using OpenTK.Graphics.OpenGL;

namespace Minecraft.Core.Textures;

public sealed class ColorTarget
{
    private int _fbo;

    public int TextureId { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public ColorTarget(int width, int height)
    {
        Create(width, height);
    }

    public void Resize(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (width == Width && height == Height)
        {
            return;
        }

        CleanUp();
        Create(width, height);
    }

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.Viewport(0, 0, Width, Height);
    }

    public void CleanUp()
    {
        GL.DeleteTexture(TextureId);
        GL.DeleteFramebuffer(_fbo);
        TextureId = 0;
        _fbo = 0;
        Width = 0;
        Height = 0;
    }

    private void Create(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);

        _fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        TextureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, TextureId);
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba16f,
            Width,
            Height,
            0,
            PixelFormat.Rgba,
            PixelType.HalfFloat,
            IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D,
            TextureId,
            0);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            Logger.Error("Colour target framebuffer is incomplete: " + status);
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}

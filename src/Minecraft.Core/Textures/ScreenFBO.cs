using Minecraft.Core.Logging;
using OpenTK.Graphics.OpenGL;

namespace Minecraft.Core.Textures;

public sealed class ScreenFBO
{
    private int _fbo;

    private int _renderBuffer;

    public int ColorTextureID { get; private set; }

    public int NormalDepthTextureID { get; private set; }

    public ScreenFBO(int screenWidth, int screenHeight)
    {
        CreateFBO(screenWidth, screenHeight);
    }

    private void CreateFBO(int screenWidth, int screenHeight)
    {
        // A minimised window reports a zero-sized client area, which would make every attachment
        // incomplete. One pixel keeps the FBO valid until the window is restored.
        screenWidth = Math.Max(1, screenWidth);
        screenHeight = Math.Max(1, screenHeight);

        // Create and bind FBO
        _fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        // Create and bind color texture
        ColorTextureID = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, ColorTextureID);
        // Set some color texture Settings
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, screenWidth, screenHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        // Sampling the edge of the screen must not wrap around to the opposite side.
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        // Attach color texture to FBO
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorTextureID, 0);

        // Create and bind normal/depth texture
        NormalDepthTextureID = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, NormalDepthTextureID);
        // Set some color texture Settings
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, screenWidth, screenHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        // Attach normal/depth texture to FBO
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, NormalDepthTextureID, 0);

        GL.BindTexture(TextureTarget.Texture2D, 0);

        // Declare both color attachments as draw buffers. This has to happen after the attachments
        // exist, otherwise the FBO is incomplete for drawing.
        DrawBuffersEnum[] buffers = new DrawBuffersEnum[2];
        for (int i = 0; i < 2; i++)
        {
            buffers[i] = DrawBuffersEnum.ColorAttachment0 + i;
        }
        GL.DrawBuffers(2, buffers);

        // Create render buffer object and bind it
        _renderBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderBuffer);

        // Set internal format to depth component
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, screenWidth, screenHeight);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _renderBuffer);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        ValidateFBO();
        UnbindFBO();
    }

    public void AdjustToWindowSize(int screenWidth, int screenHeight)
    {
        CleanUp();
        CreateFBO(screenWidth, screenHeight);
    }

    public void BindFBO()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
    }

    public void UnbindFBO()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void CleanUp()
    {
        GL.DeleteTexture(ColorTextureID);
        GL.DeleteTexture(NormalDepthTextureID);
        GL.DeleteRenderbuffer(_renderBuffer);
        GL.DeleteFramebuffer(_fbo);

        ColorTextureID = 0;
        NormalDepthTextureID = 0;
        _renderBuffer = 0;
        _fbo = 0;
    }

    private static bool ValidateFBO()
    {
        var code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        switch (code)
        {
            case FramebufferErrorCode.FramebufferComplete:
                Logger.Info("FBO: The framebuffer is complete and valid for rendering.");
                return true;
            case FramebufferErrorCode.FramebufferIncompleteAttachment:
                Logger.Error("FBO: One or more attachment points are not framebuffer attachment complete. This could mean there’s no texture attached or the format isn’t renderable. For color textures this means the base format must be RGB or RGBA and for depth textures it must be a DEPTH_COMPONENT format. Other causes of this error are that the width or height is zero or the z-offset is out of range in case of render to volume.");
                break;
            case FramebufferErrorCode.FramebufferIncompleteMissingAttachment:
                Logger.Error("FBO: There are no attachments.");
                break;
            case FramebufferErrorCode.FramebufferIncompleteDimensionsExt:
                Logger.Error("FBO: Attachments are of different size. All attachments must have the same width and height.");
                break;
            case FramebufferErrorCode.FramebufferIncompleteFormatsExt:
                Logger.Error("FBO: The color attachments have different format. All color attachments must have the same format.");
                break;
            case FramebufferErrorCode.FramebufferIncompleteDrawBuffer:
                Logger.Error("FBO: An attachment point referenced by GL.DrawBuffers() doesn’t have an attachment.");
                break;
            case FramebufferErrorCode.FramebufferIncompleteReadBuffer:
                Logger.Error("FBO: The attachment point referenced by GL.ReadBuffers() doesn’t have an attachment.");
                break;
            case FramebufferErrorCode.FramebufferUnsupported:
                Logger.Error("FBO: This particular FBO configuration is not supported by the implementation.");
                break;
            default:
                Logger.Error("FBO: Status unknown: " + code);
                break;
        }
        return false;
    }
}

using Minecraft.Core.Logging;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public sealed class ShadowMap
{
    public const int Resolution = 2048;

    public const float HalfExtent = 80F;

    private const float DepthHalfRange = 192F;

    private const float ForwardShift = 0.4F;

    private readonly int _fbo;

    public int DepthTextureId { get; }

    public Matrix4 LightViewProjection { get; private set; } = Matrix4.Identity;

    public static float TexelSize => 1.0F / Resolution;

    public ShadowMap()
    {
        DepthTextureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, DepthTextureId);
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.DepthComponent24,
            Resolution,
            Resolution,
            0,
            PixelFormat.DepthComponent,
            PixelType.Float,
            IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureCompareMode,
            (int)TextureCompareMode.CompareRefToTexture);
        GL.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureCompareFunc,
            (int)DepthFunction.Lequal);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        _fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D,
            DepthTextureId,
            0);
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            Logger.Error("Shadow map framebuffer is incomplete: " + status);
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Update(Vector3 cameraPosition, Vector3 cameraForward, Vector3 lightDirection)
    {
        LightViewProjection = ComputeLightViewProjection(cameraPosition, cameraForward, lightDirection);
    }

    public static Matrix4 ComputeLightViewProjection(
        Vector3 cameraPosition,
        Vector3 cameraForward,
        Vector3 lightDirection)
    {
        var flatForward = new Vector3(cameraForward.X, 0, cameraForward.Z);
        Vector3 center = cameraPosition;
        if (flatForward.LengthSquared > 0.0001F)
        {
            center += flatForward.Normalized() * (HalfExtent * ForwardShift);
        }

        Vector3 up = MathF.Abs(lightDirection.Z) > 0.9F ? Vector3.UnitX : Vector3.UnitZ;
        Matrix4 view = Matrix4.LookAt(Vector3.Zero, -lightDirection, up);

        Vector3 lightSpaceCenter = Vector3.TransformPosition(center, view);
        float texelWorldSize = 2.0F * HalfExtent / Resolution;
        float x = MathF.Floor(lightSpaceCenter.X / texelWorldSize) * texelWorldSize;
        float y = MathF.Floor(lightSpaceCenter.Y / texelWorldSize) * texelWorldSize;
        float depth = -lightSpaceCenter.Z;

        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(
            x - HalfExtent,
            x + HalfExtent,
            y - HalfExtent,
            y + HalfExtent,
            depth - DepthHalfRange,
            depth + DepthHalfRange);

        return view * projection;
    }

    public bool Covers(Vector3 min, Vector3 max)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int corner = 0; corner < 8; corner++)
        {
            var point = new Vector4(
                (corner & 1) == 0 ? min.X : max.X,
                (corner & 2) == 0 ? min.Y : max.Y,
                (corner & 4) == 0 ? min.Z : max.Z,
                1.0F);
            Vector4 projected = point * LightViewProjection;
            minX = MathF.Min(minX, projected.X);
            minY = MathF.Min(minY, projected.Y);
            maxX = MathF.Max(maxX, projected.X);
            maxY = MathF.Max(maxY, projected.Y);
        }

        return maxX >= -1 && minX <= 1 && maxY >= -1 && minY <= 1;
    }

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.Viewport(0, 0, Resolution, Resolution);
        GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    public void CleanUp()
    {
        GL.DeleteFramebuffer(_fbo);
        GL.DeleteTexture(DepthTextureId);
    }
}

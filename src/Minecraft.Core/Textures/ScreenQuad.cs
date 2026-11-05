using Minecraft.Core.Shaders.PostRenderShader;
using Minecraft.Core.Shaders.RealisticShader;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace Minecraft.Core.Textures;

public sealed class ScreenQuad
{
    private const int BloomDownscale = 4;
    private const int BloomBlurPasses = 2;
    private const float BloomThreshold = 1.05F;
    private const float BloomStrength = 0.3F;
    private const float Exposure = 1.0F;

    private readonly ScreenFBO _fbo;
    private readonly PostRenderShader _shader;

    private RealisticPostShader? _realisticShader;
    private BloomExtractShader? _bloomExtractShader;
    private BloomBlurShader? _bloomBlurShader;
    private ColorTarget? _bloomTarget;
    private ColorTarget? _bloomScratchTarget;

    private readonly int _vao, _vbo;
    private readonly float[] _quadVertices =
    [
        -1.0f,  1.0f, 0.0f,  0.0f, 1.0f,
        -1.0f, -1.0f, 0.0f,  0.0f, 0.0f,
        1.0f, -1.0f, 0.0f,  1.0f, 0.0f,

        -1.0f,  1.0f, 0.0f,  0.0f, 1.0f,
        1.0f, -1.0f, 0.0f,  1.0f, 0.0f,
        1.0f,  1.0f, 0.0f,  1.0f, 1.0f
    ];

    public ScreenQuad(GameWindow window)
    {
        GL.GenVertexArrays(1, out _vao);
        GL.GenBuffers(1, out _vbo);
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(_quadVertices.Length * sizeof(float)), _quadVertices, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        _shader = new PostRenderShader();
        _fbo = new ScreenFBO(window.ClientSize.X, window.ClientSize.Y);
    }

    public void RenderToScreen()
    {
        _shader.Start();
        _shader.LoadTexture(_shader.LocationColorTexture, 0, _fbo.ColorTextureID);
        _shader.LoadTexture(_shader.LocationNormalDepthTexture, 1, _fbo.NormalDepthTextureID);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
        _shader.Stop();
    }

    public int Width => _fbo.Width;

    public int Height => _fbo.Height;

    public void SetRealistic(bool realistic)
    {
        _fbo.SetHighDynamicRange(realistic);

        if (!realistic)
        {
            ReleaseBloomTargets();
            return;
        }

        _realisticShader ??= new RealisticPostShader();
        _bloomExtractShader ??= new BloomExtractShader();
        _bloomBlurShader ??= new BloomBlurShader();
        _bloomTarget ??= new ColorTarget(BloomWidth, BloomHeight);
        _bloomScratchTarget ??= new ColorTarget(BloomWidth, BloomHeight);
    }

    public void RenderRealisticToScreen()
    {
        if (_realisticShader is null ||
            _bloomExtractShader is null ||
            _bloomBlurShader is null ||
            _bloomTarget is null ||
            _bloomScratchTarget is null)
        {
            RenderToScreen();
            return;
        }

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.DepthTest);
        GL.BindVertexArray(_vao);

        _bloomTarget.Bind();
        _bloomExtractShader.Start();
        _bloomExtractShader.LoadTexture(_bloomExtractShader.LocationSourceTexture, 0, _fbo.ColorTextureID);
        _bloomExtractShader.LoadVector(
            _bloomExtractShader.LocationSourceTexelSize,
            new Vector2(1.0F / _fbo.Width, 1.0F / _fbo.Height));
        _bloomExtractShader.LoadFloat(_bloomExtractShader.LocationThreshold, BloomThreshold);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _bloomBlurShader.Start();
        for (int pass = 0; pass < BloomBlurPasses; pass++)
        {
            float spread = pass + 1;
            BlurInto(_bloomScratchTarget, _bloomTarget, new Vector2(spread / _bloomTarget.Width, 0));
            BlurInto(_bloomTarget, _bloomScratchTarget, new Vector2(0, spread / _bloomTarget.Height));
        }

        _fbo.UnbindFBO();
        GL.Viewport(0, 0, _fbo.Width, _fbo.Height);

        _realisticShader.Start();
        _realisticShader.LoadTexture(_realisticShader.LocationColorTexture, 0, _fbo.ColorTextureID);
        _realisticShader.LoadTexture(_realisticShader.LocationBloomTexture, 1, _bloomTarget.TextureId);
        _realisticShader.LoadFloat(_realisticShader.LocationExposure, Exposure);
        _realisticShader.LoadFloat(_realisticShader.LocationBloomStrength, BloomStrength);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);
        _realisticShader.Stop();
        GL.ActiveTexture(TextureUnit.Texture0);
    }

    private void BlurInto(ColorTarget destination, ColorTarget source, Vector2 blurStep)
    {
        destination.Bind();
        _bloomBlurShader!.LoadTexture(_bloomBlurShader.LocationSourceTexture, 0, source.TextureId);
        _bloomBlurShader.LoadVector(_bloomBlurShader.LocationBlurStep, blurStep);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    private int BloomWidth => Math.Max(1, _fbo.Width / BloomDownscale);

    private int BloomHeight => Math.Max(1, _fbo.Height / BloomDownscale);

    private void ReleaseBloomTargets()
    {
        _bloomTarget?.CleanUp();
        _bloomScratchTarget?.CleanUp();
        _bloomTarget = null;
        _bloomScratchTarget = null;
    }

    public void CleanUp()
    {
        ReleaseBloomTargets();
        _realisticShader?.CleanUp();
        _bloomExtractShader?.CleanUp();
        _bloomBlurShader?.CleanUp();
        _fbo.CleanUp();
        _shader.CleanUp();
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
    }

    public void AdjustToWindowSize(int screenWidth, int screenHeight)
    {
        _fbo.AdjustToWindowSize(screenWidth, screenHeight);
        _bloomTarget?.Resize(BloomWidth, BloomHeight);
        _bloomScratchTarget?.Resize(BloomWidth, BloomHeight);
    }

    public void Bind()
    {
        _fbo.BindFBO();
    }

    public void Unbind()
    {
        _fbo.UnbindFBO();
    }
}

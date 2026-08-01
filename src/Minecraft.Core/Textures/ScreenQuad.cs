using Minecraft.Core.Shaders.PostRenderShader;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Desktop;

namespace Minecraft.Core.Textures;

public sealed class ScreenQuad
{
    private readonly ScreenFBO _fbo;
    private readonly PostRenderShader _shader;

    private readonly int _vao, _vbo;
    private readonly float[] _quadVertices =
    [
        // positions   // texCoords
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
        _shader.Stop();
    }

    public void AdjustToWindowSize(int screenWidth, int screenHeight)
    {
        _fbo.AdjustToWindowSize(screenWidth, screenHeight);
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

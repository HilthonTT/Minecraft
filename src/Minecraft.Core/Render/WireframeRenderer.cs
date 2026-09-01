using Minecraft.Core.Entities;
using Minecraft.Core.Shaders.WireframeShader;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public sealed class WireframeRenderer
{
    private readonly WireframeShader _shader = new();
    private readonly MasterRenderer _renderer;
    private readonly VAOModel _aabbCube;

    private readonly float _maxLineWidth;

    public WireframeRenderer(MasterRenderer renderer)
    {
        _renderer = renderer;
        _aabbCube = CreateDefaultAABBCube();

        var lineWidthRange = new float[2];
        GL.GetFloat(GetPName.AliasedLineWidthRange, lineWidthRange);
        _maxLineWidth = Math.Max(1F, lineWidthRange[1]);
    }

    private static VAOModel CreateDefaultAABBCube()
    {
        float[] positions =
        [
            0, 0, 0,
            1, 0, 0,
            1, 0, 1,
            0, 0, 1,
            0, 1, 0,
            1, 1, 0,
            1, 1, 1,
            0, 1, 1,
        ];
        int[] indices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7,
        ];
        return new VAOModel(positions, indices);
    }

    public void RenderWireframeAt(int lineWidth, Vector3 translation, Vector3 scale, Vector3 color)
    {
        const float dt = 0.001F;
        Vector3 offset = new(dt, dt, dt);
        scale += offset;
        translation -= (offset / 2);

        GL.LineWidth(Math.Clamp(lineWidth, 1F, _maxLineWidth));

        Camera activeCamera = _renderer.GetActiveCamera();

        _shader.Start();
        _shader.LoadMatrix(_shader.LocationViewMatrix, activeCamera.CurrentViewMatrix);
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, activeCamera.CurrentProjectionMatrix);
        _shader.LoadVector(_shader.LocationColor, color);

        Matrix4 transformMatrix = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(translation);
        _shader.LoadMatrix(_shader.LocationTransformationMatrix, Matrix4.Identity * transformMatrix);
        _aabbCube.BindVAO();
        GL.DrawElements(PrimitiveType.Lines, _aabbCube.IndicesCount, DrawElementsType.UnsignedInt, 0);
        VAOModel.UnbindVAO();
        _shader.Stop();
    }

    public void CleanUp()
    {
        _aabbCube.CleanUp();
    }
}

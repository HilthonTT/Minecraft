using Minecraft.Core.Entities;
using Minecraft.Core.Shaders.WireframeShader;
using Minecraft.Core.Utilities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public sealed class WireframeRenderer
{
    private readonly WireframeShader _shader = new();
    private readonly MasterRenderer _renderer;
    private readonly VAOModel _aabbCube;

    /// <summary>The widest line this driver will draw. Only a width of one is guaranteed to be available.</summary>
    private readonly float _maxLineWidth;

    public WireframeRenderer(MasterRenderer renderer)
    {
        _renderer = renderer;
        _aabbCube = CreateDefaultAABBCube();

        var lineWidthRange = new float[2];
        GL.GetFloat(GetPName.AliasedLineWidthRange, lineWidthRange);
        _maxLineWidth = Math.Max(1F, lineWidthRange[1]);
    }

    /// <summary>
    /// The twelve edges of a unit cube, as a list of lines.
    /// <para>
    /// Drawn as lines outright rather than as filled quads switched to a line polygon mode. Quads are not in
    /// the core profile and a driver that holds to that rejects them, which leaves nothing drawn at all
    /// rather than anything visibly wrong to chase.
    /// </para>
    /// </summary>
    private static VAOModel CreateDefaultAABBCube()
    {
        //Direction are relative to facing positive Z
        float[] positions =
        [
            //Bottom points (looking from top down)
            0, 0, 0, //Bottom-right   -- index 0
            1, 0, 0, //Bottom-left    -- index 1
            1, 0, 1, //Top-left       -- index 2
            0, 0, 1, //Top-right      -- index 3
            //Top points    (looking from top down)
            0, 1, 0, //Bottom-right   -- index 4
            1, 1, 0, //Bottom-left    -- index 5
            1, 1, 1, //Top-left       -- index 6
            0, 1, 1, //Top-right      -- index 7
        ];
        int[] indices =
        [
            0, 1, 1, 2, 2, 3, 3, 0, //Bottom ring
            4, 5, 5, 6, 6, 7, 7, 4, //Top ring
            0, 4, 1, 5, 2, 6, 3, 7, //The uprights joining the two
        ];
        return new VAOModel(positions, indices);
    }

    /// <summary> Draws a cube wireframe at the given location. Scale is relative to a 1x1x1 cube. </summary>
    public void RenderWireframeAt(int lineWidth, Vector3 translation, Vector3 scale, Vector3 color)
    {
        const float dt = 0.001F;
        Vector3 offset = new(dt, dt, dt);
        scale += offset;
        translation -= (offset / 2);

        // Anything above one is optional in a core profile, and asking for more than the driver offers is an
        // error that leaves the width wherever it happened to be.
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

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

    public WireframeRenderer(MasterRenderer renderer)
    {
        _renderer = renderer;
        _aabbCube = CreateDefaultAABBCube();
    }

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
            0, 1, 2, 3, //Bottom
            7, 6, 5, 4, //Top
            3, 7, 4, 0, //Right
            2, 1, 5, 6, //Left
            0, 4, 5, 1, //Front
            3, 2, 6, 7  //Back
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

        GL.LineWidth(lineWidth);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

        Camera activeCamera = _renderer.GetActiveCamera();

        _shader.Start();
        _shader.LoadMatrix(_shader.LocationViewMatrix, activeCamera.CurrentViewMatrix);
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, activeCamera.CurrentProjectionMatrix);
        _shader.LoadVector(_shader.LocationColor, color);

        Matrix4 transformMatrix = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(translation);
        _shader.LoadMatrix(_shader.LocationTransformationMatrix, Matrix4.Identity * transformMatrix);
        _aabbCube.BindVAO();
        GL.DrawElements(PrimitiveType.Quads, _aabbCube.IndicesCount, DrawElementsType.UnsignedInt, 0);
        VAOModel.UnbindVAO();
        _shader.Stop();

        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    public void CleanUp()
    {
        _aabbCube.CleanUp();
    }
}

using Minecraft.Core.Shaders.UIShader;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

/// <summary>A single textured quad on a canvas, positioned and sized in canvas pixels.</summary>
public sealed class UIImage : UIComponent
{
    private readonly Vector2 _dimension;

    private Texture _texture;

    public Texture Texture
    {
        get => _texture;
        set
        {
            if (_texture.Id == value.Id)
            {
                return;
            }

            _texture = value;
            ParentCanvas.AddComponentToClean(this);
        }
    }

    public UIImage(UICanvas parentCanvas, Vector2 position, Vector2 dimension, Texture texture)
        : base(parentCanvas, position)
    {
        _texture = texture;
        _dimension = dimension;

        // The quad geometry depends on the canvas size, so it is built on the next clean pass.
        parentCanvas.AddComponentToClean(this);
    }

    public override void Clean()
    {
        // Canvas pixels have their origin at the top left, normalised device coordinates at the centre with
        // Y pointing up, hence the flip on the vertical axis.
        float xNdc = PixelPositionInCanvas.X / ParentCanvas.PixelWidth * 2 - 1;
        float yNdc = 1 - PixelPositionInCanvas.Y / ParentCanvas.PixelHeight * 2;
        float width = 2 * _dimension.X / ParentCanvas.PixelWidth;
        float height = 2 * -_dimension.Y / ParentCanvas.PixelHeight;

        Vector3 topLeft = new(xNdc, yNdc, 0);
        Vector3 bottomLeft = new(xNdc, yNdc + height, 0);
        Vector3 bottomRight = new(xNdc + width, yNdc + height, 0);
        Vector3 topRight = new(xNdc + width, yNdc, 0);

        float[] vertices =
        [
            bottomLeft.X, bottomLeft.Y, bottomLeft.Z,
            bottomRight.X, bottomRight.Y, bottomRight.Z,
            topRight.X, topRight.Y, topRight.Z,
            bottomLeft.X, bottomLeft.Y, bottomLeft.Z,
            topRight.X, topRight.Y, topRight.Z,
            topLeft.X, topLeft.Y, topLeft.Z,
        ];

        float[] textureCoords =
        [
            0, 1, // bottom-left
            1, 1, // bottom-right
            1, 0, // top-right
            0, 1, // bottom-left
            1, 0, // top-right
            0, 0, // top-left
        ];

        _vaoModel?.CleanUp();
        _vaoModel = new VAOModel(vertices, textureCoords, 6);
    }

    public override void Render(UIShader uiShader)
    {
        if (_vaoModel == null)
        {
            return;
        }

        base.Render(uiShader);
        _vaoModel.BindVAO();
        uiShader.LoadTexture(uiShader.LocationTexture, 0, Texture.Id);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _vaoModel.IndicesCount);
    }
}

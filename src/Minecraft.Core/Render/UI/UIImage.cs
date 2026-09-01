using Minecraft.Core.Shaders.UIShader;
using Minecraft.Core.Textures;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UIImage : UIComponent
{
    private Vector2 _dimension;

    private Texture _texture;

    public Vector2 Dimension
    {
        get => _dimension;
        set
        {
            if (_dimension == value)
            {
                return;
            }

            _dimension = value;
            ParentCanvas.AddComponentToClean(this);
        }
    }

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

        parentCanvas.AddComponentToClean(this);
    }

    public override void Clean()
    {
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
            0, 1,
            1, 1,
            1, 0,
            0, 1,
            1, 0,
            0, 0,
        ];

        _vaoModel?.CleanUp();
        _vaoModel = new VAOModel(vertices, textureCoords, 6);
    }

    public override void Render(UIShader uiShader)
    {
        if (_vaoModel is null)
        {
            return;
        }

        base.Render(uiShader);
        _vaoModel.BindVAO();
        uiShader.LoadTexture(uiShader.LocationTexture, 0, Texture.Id);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _vaoModel.IndicesCount);
    }
}

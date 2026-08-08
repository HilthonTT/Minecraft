using Minecraft.Core.Shaders.UIShader;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

/// <summary>
/// A run of text on a canvas. Changing the text does not rebuild the mesh immediately; the component is
/// queued with its canvas and rebuilt once per frame, so setting it repeatedly costs nothing extra.
/// </summary>
public sealed class UIText : UIComponent
{
    private readonly TextMeshBuilder _meshBuilder = new();

    private string _text;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            ParentCanvas.AddComponentToClean(this);
        }
    }

    public Font Font { get; }

    public Vector2 Scale { get; }

    public UIText(UICanvas parentCanvas, Font font, Vector2 position, Vector2 scale, string text)
        : base(parentCanvas, position)
    {
        Font = font;
        Scale = scale;

        // Set through the backing field first, so the property setter below sees a change even when the
        // initial text is empty and always queues the first mesh build.
        _text = string.Empty;
        Text = text;
        parentCanvas.AddComponentToClean(this);
    }

    public override void Clean()
    {
        float[] vertices = _meshBuilder.GetVerticesForText(this);
        float[] textureCoords = _meshBuilder.GetTexturesForText(this);
        int indicesCount = vertices.Length / 3;

        _vaoModel?.CleanUp();
        _vaoModel = new VAOModel(vertices, textureCoords, indicesCount);
    }

    public override void Render(UIShader uiShader)
    {
        if (_vaoModel is null)
        {
            return;
        }

        base.Render(uiShader);
        _vaoModel.BindVAO();
        uiShader.LoadTexture(uiShader.LocationTexture, 0, Font.FontMapTexture.Id);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _vaoModel.IndicesCount);
    }
}

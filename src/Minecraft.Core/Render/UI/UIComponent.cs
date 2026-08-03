using Minecraft.Core.Shaders.UIShader;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public abstract class UIComponent
{
    private Vector2 _pixelPositionInCanvas;

    public UICanvas ParentCanvas { get; private set; }

    /// <summary>
    /// The top left corner of the component in canvas pixels. Moving it does not rebuild the mesh straight
    /// away; the component is queued with its canvas and rebuilt once per frame.
    /// </summary>
    public Vector2 PixelPositionInCanvas
    {
        get => _pixelPositionInCanvas;
        set
        {
            if (_pixelPositionInCanvas == value)
            {
                return;
            }

            _pixelPositionInCanvas = value;
            ParentCanvas.AddComponentToClean(this);
        }
    }

    /// <summary>Whether the component takes part in rendering. Hiding one keeps its mesh around.</summary>
    public bool IsVisible { get; set; } = true;

    public float Transparency { get; set; } = 1.0F;
    public Vector3 Color { get; set; } = Vector3.One;
    protected VAOModel? _vaoModel;

    protected UIComponent(UICanvas parentCanvas, Vector2 pixelPositionInCanvas)
    {
        ParentCanvas = parentCanvas;
        _pixelPositionInCanvas = pixelPositionInCanvas;
    }

    public abstract void Clean();

    public virtual void Render(UIShader uiShader)
    {
        uiShader.LoadFloat(uiShader.LocationTransparency, Transparency);
        uiShader.LoadVector(uiShader.LocationColor, Color);
    }
}

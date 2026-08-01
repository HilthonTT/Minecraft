using Minecraft.Core.Shaders.UIShader;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public abstract class UIComponent
{
    public UICanvas ParentCanvas { get; private set; }
    public Vector2 PixelPositionInCanvas { get; private set; }
    public float Transparency { get; set; } = 1.0F;
    public Vector3 Color { get; set; } = Vector3.One;
    protected VAOModel? _vaoModel;

    protected UIComponent(UICanvas parentCanvas, Vector2 pixelPositionInCanvas)
    {
        ParentCanvas = parentCanvas;
        PixelPositionInCanvas = pixelPositionInCanvas;
    }

    public abstract void Clean();

    public virtual void Render(UIShader uiShader)
    {
        uiShader.LoadFloat(uiShader.LocationTransparency, Transparency);
        uiShader.LoadVector(uiShader.LocationColor, Color);
    }
}
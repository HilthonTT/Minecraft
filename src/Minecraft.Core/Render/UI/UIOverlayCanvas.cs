using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UIOverlayCanvas : UICanvas
{
    public UIOverlayCanvas(int pixelWidth, int pixelHeight)
        : base(Vector3.Zero, Vector3.Zero, pixelWidth, pixelHeight, RenderSpace.Screen)
    {
        IsOverlay = true;
    }
}

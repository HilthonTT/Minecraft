using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

/// <summary>
/// A screen canvas drawn after the block icons rather than before them. It holds nothing of its own: screens
/// that draw slots put the parts which have to sit over a block — a count, a label — onto one of these and
/// keep the panels behind them on a canvas of their own.
/// </summary>
public sealed class UIOverlayCanvas : UICanvas
{
    public UIOverlayCanvas(int pixelWidth, int pixelHeight)
        : base(Vector3.Zero, Vector3.Zero, pixelWidth, pixelHeight, RenderSpace.Screen)
    {
        IsOverlay = true;
    }
}

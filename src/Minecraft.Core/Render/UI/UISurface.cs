using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UISurface
{
    private const float BevelHeight = 2F;
    private const float BevelTransparency = 0.14F;
    private const float LipHeight = 3F;
    private const float LipTransparency = 0.30F;

    private readonly UIImage _outline;
    private readonly UIImage _fill;
    private readonly UIImage _bevel;
    private readonly UIImage _lip;

    public UISurface(UICanvas canvas)
    {
        _outline = UIDraw.Rect(canvas, UITheme.Outline);
        _fill = UIDraw.Rect(canvas, UITheme.Surface, 1F, UITextures.Gradient);
        _bevel = UIDraw.Rect(canvas, Vector3.One, BevelTransparency);
        _lip = UIDraw.Rect(canvas, Vector3.Zero, LipTransparency);
    }

    public bool IsVisible
    {
        set
        {
            _outline.IsVisible = value;
            _fill.IsVisible = value;
            _bevel.IsVisible = value;
            _lip.IsVisible = value;
        }
    }

    public void Layout(Vector2 position, Vector2 size)
    {
        float border = UITheme.BorderWidth;
        Vector2 inner = position + new Vector2(border, border);
        Vector2 innerSize = size - new Vector2(2 * border, 2 * border);

        UIDraw.Place(_outline, position, size);
        UIDraw.Place(_fill, inner, innerSize);
        UIDraw.Place(_bevel, inner, new Vector2(innerSize.X, BevelHeight));
        UIDraw.Place(_lip, new Vector2(inner.X, inner.Y + innerSize.Y - LipHeight), new Vector2(innerSize.X, LipHeight));
    }

    public void Paint(Vector3 fill, Vector3 outline, bool raised = true)
    {
        _fill.Color = fill;
        _outline.Color = outline;
        _bevel.Transparency = raised ? BevelTransparency : 0.03F;
        _lip.Transparency = raised ? LipTransparency : 0.08F;
    }
}

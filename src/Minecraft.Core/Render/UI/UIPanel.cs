using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UIPanel
{
    private const float HighlightHeight = 2F;

    private readonly UIImage? _shadow;
    private readonly UIImage _border;
    private readonly UIImage _fill;
    private readonly UIImage _highlight;

    private readonly float _fillTransparency;
    private float _transparency = 1F;
    private bool _isVisible = true;

    public UIPanel(
        UICanvas canvas,
        bool shadow = true,
        Vector3? fill = null,
        Vector3? border = null,
        float fillTransparency = UITheme.PanelTransparency)
    {
        _fillTransparency = fillTransparency;

        if (shadow)
        {
            _shadow = UIDraw.Rect(canvas, UITheme.Shadow, UITheme.ShadowTransparency);
        }

        _border = UIDraw.Rect(canvas, border ?? UITheme.PanelBorder, fillTransparency);
        _fill = UIDraw.Rect(canvas, fill ?? UITheme.Panel, fillTransparency);
        _highlight = UIDraw.Rect(canvas, Vector3.One, 0.05F);
    }

    public Vector2 Position { get; private set; }

    public Vector2 Size { get; private set; }

    public Vector3 FillColor
    {
        get => _fill.Color;
        set => _fill.Color = value;
    }

    public Vector3 BorderColor
    {
        get => _border.Color;
        set => _border.Color = value;
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            if (_shadow is not null)
            {
                _shadow.IsVisible = value;
            }

            _border.IsVisible = value;
            _fill.IsVisible = value;
            _highlight.IsVisible = value;
        }
    }

    public float Transparency
    {
        get => _transparency;
        set
        {
            _transparency = value;
            if (_shadow is not null)
            {
                _shadow.Transparency = UITheme.ShadowTransparency * value;
            }

            _border.Transparency = _fillTransparency * value;
            _fill.Transparency = _fillTransparency * value;
            _highlight.Transparency = 0.05F * value;
        }
    }

    public void SetBounds(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;

        float border = UITheme.BorderWidth;

        if (_shadow is not null)
        {
            UIDraw.Place(_shadow, position + new Vector2(UITheme.ShadowOffset, UITheme.ShadowOffset), size);
        }

        UIDraw.Place(_border, position, size);
        UIDraw.Place(_fill, position + new Vector2(border, border), size - new Vector2(2 * border, 2 * border));
        UIDraw.Place(_highlight, position + new Vector2(border, border), new Vector2(size.X - (2 * border), HighlightHeight));
    }

    public void SetBounds(float left, float top, float width, float height)
    {
        SetBounds(new Vector2(left, top), new Vector2(width, height));
    }

    public bool Contains(Vector2 point)
    {
        return _isVisible &&
               point.X >= Position.X && point.X <= Position.X + Size.X &&
               point.Y >= Position.Y && point.Y <= Position.Y + Size.Y;
    }
}

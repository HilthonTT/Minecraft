using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

/// <summary>
/// A clickable box with its label centred in it. A canvas draws components rather than widgets, so a button
/// is not one itself: it owns a panel and a run of text on the canvas it was given and moves them together.
/// </summary>
public sealed class UIButton
{
    /// <summary>How tall a button is, and how far apart two of them sit, in canvas pixels.</summary>
    public const int Height = 48;
    public const int Gap = 14;

    private const float LabelScale = 0.38F;

    private static readonly Vector3 _idleColor = new(0.24F, 0.26F, 0.31F);
    private static readonly Vector3 _hoverColor = new(0.36F, 0.41F, 0.48F);
    private static readonly Vector3 _disabledColor = new(0.16F, 0.17F, 0.19F);
    private static readonly Vector3 _labelColor = new(0.95F, 0.95F, 0.95F);
    private static readonly Vector3 _disabledLabelColor = new(0.55F, 0.55F, 0.55F);

    private readonly Font _font;
    private readonly UIImage _panel;
    private readonly UIText _label;

    private Vector2 _position;
    private Vector2 _size;

    /// <summary>Whether the button reacts to the mouse. A disabled one is drawn greyed out.</summary>
    public bool IsEnabled { get; set; } = true;

    public bool IsVisible
    {
        get => _panel.IsVisible;
        set
        {
            _panel.IsVisible = value;
            _label.IsVisible = value;
        }
    }

    public string Text
    {
        get => _label.Text;
        set
        {
            _label.Text = value;
            LayoutLabel();
        }
    }

    public UIButton(UICanvas canvas, string text)
    {
        _font = FontRegistry.GetFont(FontType.Arial);

        // Panel first and label second, since a canvas draws its components in the order it was given them.
        _panel = new UIImage(canvas, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = _idleColor,
        };
        canvas.AddComponentToRender(_panel);

        _label = new UIText(canvas, _font, Vector2.Zero, new Vector2(LabelScale, LabelScale), text)
        {
            Color = _labelColor,
        };
        canvas.AddComponentToRender(_label);
    }

    public void SetBounds(Vector2 position, Vector2 size)
    {
        _position = position;
        _size = size;

        _panel.PixelPositionInCanvas = position;
        _panel.Dimension = size;
        LayoutLabel();
    }

    /// <summary>
    /// Updates the highlight for where the mouse is and reports whether the button was clicked this frame.
    /// </summary>
    public bool Update(Vector2 mousePosition, bool mousePressed)
    {
        if (!IsEnabled)
        {
            _panel.Color = _disabledColor;
            _label.Color = _disabledLabelColor;
            return false;
        }

        bool isHovered = Contains(mousePosition);
        _panel.Color = isHovered ? _hoverColor : _idleColor;
        _label.Color = _labelColor;

        return isHovered && mousePressed && IsVisible;
    }

    public bool Contains(Vector2 point)
    {
        return point.X >= _position.X && point.X <= _position.X + _size.X &&
               point.Y >= _position.Y && point.Y <= _position.Y + _size.Y;
    }

    private void LayoutLabel()
    {
        float labelWidth = _font.MeasureWidth(_label.Text, LabelScale);
        (float glyphTop, float glyphBottom) = _font.MeasureVerticalBounds(_label.Text, LabelScale);

        // The component's position is where the text box starts, not where its glyphs do, so the offset the
        // glyphs hang by is taken back out to leave them sitting in the middle of the button.
        _label.PixelPositionInCanvas = new Vector2(
            _position.X + ((_size.X - labelWidth) / 2.0F),
            _position.Y + ((_size.Y - (glyphBottom - glyphTop)) / 2.0F) - glyphTop);
    }
}

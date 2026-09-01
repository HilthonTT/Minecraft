using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UIButton
{
    public const int Height = 48;
    public const int Gap = 14;

    private const float LabelScale = 0.38F;

    private const float LabelPaddingPixels = 10;

    private const string Ellipsis = "...";

    private static readonly Vector3 _idleColor = new(0.24F, 0.26F, 0.31F);
    private static readonly Vector3 _hoverColor = new(0.36F, 0.41F, 0.48F);
    private static readonly Vector3 _destructiveIdleColor = new(0.38F, 0.16F, 0.16F);
    private static readonly Vector3 _destructiveHoverColor = new(0.56F, 0.22F, 0.22F);
    private static readonly Vector3 _disabledColor = new(0.16F, 0.17F, 0.19F);
    private static readonly Vector3 _labelColor = new(0.95F, 0.95F, 0.95F);
    private static readonly Vector3 _disabledLabelColor = new(0.55F, 0.55F, 0.55F);

    private readonly Font _font;
    private readonly UIImage _panel;
    private readonly UIText _label;

    private Vector2 _position;
    private Vector2 _size;

    private string _text;

    public bool IsEnabled { get; set; } = true;

    public bool IsDestructive { get; set; }

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
        get => _text;
        set
        {
            _text = value;
            LayoutLabel();
        }
    }

    public static float MeasureRequiredWidth(Font font, string text)
    {
        return font.MeasureWidth(text, LabelScale) + (2 * LabelPaddingPixels);
    }

    public UIButton(UICanvas canvas, string text)
    {
        _text = text;
        _font = FontRegistry.GetFont(FontType.Arial);

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

    public bool Update(Vector2 mousePosition, bool mousePressed)
    {
        if (!IsEnabled)
        {
            _panel.Color = _disabledColor;
            _label.Color = _disabledLabelColor;
            return false;
        }

        bool isHovered = Contains(mousePosition);
        _panel.Color = (isHovered, IsDestructive) switch
        {
            (true, true) => _destructiveHoverColor,
            (true, false) => _hoverColor,
            (false, true) => _destructiveIdleColor,
            (false, false) => _idleColor,
        };
        _label.Color = _labelColor;

        return isHovered && mousePressed && IsVisible;
    }

    public bool Contains(Vector2 point)
    {
        return point.X >= _position.X && point.X <= _position.X + _size.X &&
               point.Y >= _position.Y && point.Y <= _position.Y + _size.Y;
    }

    private string FitLabel(string text)
    {
        float available = _size.X - (2 * LabelPaddingPixels);

        if (available <= 0 || _font.MeasureWidth(text, LabelScale) <= available)
        {
            return text;
        }

        float ellipsisWidth = _font.MeasureWidth(Ellipsis, LabelScale);

        int fitting = text.Length;
        while (fitting > 0 && _font.MeasureWidth(text[..fitting], LabelScale) + ellipsisWidth > available)
        {
            fitting--;
        }

        return fitting == 0 ? Ellipsis : text[..fitting] + Ellipsis;
    }

    private void LayoutLabel()
    {
        _label.Text = FitLabel(_text);

        float labelWidth = _font.MeasureWidth(_label.Text, LabelScale);
        (float glyphTop, float glyphBottom) = _font.MeasureVerticalBounds(_label.Text, LabelScale);

        _label.PixelPositionInCanvas = new Vector2(
            _position.X + ((_size.X - labelWidth) / 2.0F),
            _position.Y + ((_size.Y - (glyphBottom - glyphTop)) / 2.0F) - glyphTop);
    }
}

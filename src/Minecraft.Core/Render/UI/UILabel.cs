using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UILabel
{
    private const string CapReference = "H";

    private readonly UIText? _shadow;
    private readonly UIText _text;

    private float _transparency = 1F;
    private bool _isVisible = true;

    public UILabel(UICanvas canvas, float scale, Vector3 color, string text = "", bool shadow = true)
    {
        Font = FontRegistry.GetFont(FontType.Arial);
        Scale = scale;

        if (shadow)
        {
            _shadow = new UIText(canvas, Font, Vector2.Zero, new Vector2(scale, scale), text)
            {
                Color = UITheme.TextShadow,
                Transparency = 0.8F,
            };
            canvas.AddComponentToRender(_shadow);
        }

        _text = new UIText(canvas, Font, Vector2.Zero, new Vector2(scale, scale), text) { Color = color };
        canvas.AddComponentToRender(_text);
    }

    public Font Font { get; }

    public float Scale { get; }

    public string Text
    {
        get => _text.Text;
        set
        {
            _text.Text = value;
            if (_shadow is not null)
            {
                _shadow.Text = value;
            }
        }
    }

    public Vector3 Color
    {
        get => _text.Color;
        set => _text.Color = value;
    }

    public float Transparency
    {
        get => _transparency;
        set
        {
            _transparency = value;
            _text.Transparency = value;
            if (_shadow is not null)
            {
                _shadow.Transparency = value * 0.8F;
            }
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            _text.IsVisible = value;
            if (_shadow is not null)
            {
                _shadow.IsVisible = value;
            }
        }
    }

    public float Width => Font.MeasureWidth(Text, Scale);

    public float LineHeight => Font.DesiredPixelLineHeight * Scale;

    public float CapTop => Font.MeasureVerticalBounds(CapReference, Scale).Top;

    public float CapHeight
    {
        get
        {
            (float top, float bottom) = Font.MeasureVerticalBounds(CapReference, Scale);
            return bottom - top;
        }
    }

    public float ShadowOffset => MathF.Max(1F, Scale * 4.5F);

    public float Measure(string text) => Font.MeasureWidth(text, Scale);

    public void PlaceLine(Vector2 lineTopLeft)
    {
        _text.PixelPositionInCanvas = lineTopLeft;
        if (_shadow is not null)
        {
            _shadow.PixelPositionInCanvas = lineTopLeft + new Vector2(ShadowOffset, ShadowOffset);
        }
    }

    public void PlaceTop(float left, float capTop) => PlaceLine(new Vector2(left, capTop - CapTop));

    public void PlaceMiddle(float left, float middle) => PlaceTop(left, middle - (CapHeight / 2F));

    public void PlaceCentredIn(Vector2 position, Vector2 size)
    {
        PlaceMiddle(position.X + ((size.X - Width) / 2F), position.Y + (size.Y / 2F));
    }

    public void SetFitted(string text, float maxWidth)
    {
        Text = UITextLayout.Ellipsize(text, maxWidth, Measure);
    }

    public void SetWrapped(string text, float maxWidth)
    {
        Text = UITextLayout.WrapToString(text, maxWidth, Measure);
    }

    public int LineCount => Text.Length == 0 ? 0 : Text.Count(character => character == '\n') + 1;

    public float LineStep => (int)(Font.DesiredPixelLineHeight * Scale);

    public float BlockHeight => LineCount == 0 ? 0F : ((LineCount - 1) * LineStep) + CapHeight;
}

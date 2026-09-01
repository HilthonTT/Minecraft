using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI;

public sealed class UISlider
{
    public const int Height = 40;
    public const int Gap = 10;

    private const float LabelScale = 0.32F;

    private const float HandleWidth = 12F;

    private static readonly Vector3 _trackColor = new(0.16F, 0.17F, 0.20F);
    private static readonly Vector3 _fillColor = new(0.26F, 0.36F, 0.44F);
    private static readonly Vector3 _handleColor = new(0.62F, 0.68F, 0.76F);
    private static readonly Vector3 _handleHoverColor = new(0.82F, 0.87F, 0.94F);
    private static readonly Vector3 _labelColor = new(0.95F, 0.95F, 0.95F);

    private readonly Font _font;
    private readonly UIImage _track;
    private readonly UIImage _fill;
    private readonly UIImage _handle;
    private readonly UIText _label;

    private readonly string _name;
    private readonly float _minimum;
    private readonly float _maximum;

    private readonly Func<float, string> _describe;

    private Vector2 _position;
    private Vector2 _size;
    private float _value;

    private bool _isDragging;

    public float Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, _minimum, _maximum);
            Layout();
        }
    }

    public UISlider(UICanvas canvas, string name, float minimum, float maximum, Func<float, string> describe)
    {
        _name = name;
        _minimum = minimum;
        _maximum = maximum;
        _describe = describe;
        _value = minimum;
        _font = FontRegistry.GetFont(FontType.Arial);

        _track = AddPanel(canvas, _trackColor);
        _fill = AddPanel(canvas, _fillColor);
        _handle = AddPanel(canvas, _handleColor);

        _label = new UIText(canvas, _font, Vector2.Zero, new Vector2(LabelScale, LabelScale), name)
        {
            Color = _labelColor,
        };
        canvas.AddComponentToRender(_label);
    }

    public void SetBounds(Vector2 position, Vector2 size)
    {
        _position = position;
        _size = size;
        Layout();
    }

    public float? Update(Vector2 mousePosition, bool mousePressed)
    {
        bool held = Game.Input.OnMouseDown(MouseButton.Left);

        if (mousePressed && Contains(mousePosition))
        {
            _isDragging = true;
        }
        else if (!held)
        {
            _isDragging = false;
        }

        bool isHovered = Contains(mousePosition);
        _handle.Color = _isDragging || isHovered ? _handleHoverColor : _handleColor;

        if (!_isDragging)
        {
            return null;
        }

        float previous = _value;
        Value = ValueAt(mousePosition.X);
        return _value == previous ? null : _value;
    }

    public bool Contains(Vector2 point)
    {
        return point.X >= _position.X && point.X <= _position.X + _size.X &&
               point.Y >= _position.Y && point.Y <= _position.Y + _size.Y;
    }

    private float ValueAt(float pixelX)
    {
        float travel = Math.Max(1F, _size.X - HandleWidth);
        float fraction = Math.Clamp((pixelX - _position.X - (HandleWidth / 2F)) / travel, 0F, 1F);
        return _minimum + (fraction * (_maximum - _minimum));
    }

    private void Layout()
    {
        float fraction = _maximum > _minimum ? (_value - _minimum) / (_maximum - _minimum) : 0F;
        float travel = Math.Max(0F, _size.X - HandleWidth);

        _track.PixelPositionInCanvas = _position;
        _track.Dimension = _size;

        _fill.PixelPositionInCanvas = _position;
        _fill.Dimension = new Vector2((HandleWidth / 2F) + (fraction * travel), _size.Y);

        _handle.PixelPositionInCanvas = new Vector2(_position.X + (fraction * travel), _position.Y);
        _handle.Dimension = new Vector2(HandleWidth, _size.Y);

        _label.Text = _name + ": " + _describe(_value);

        float labelWidth = _font.MeasureWidth(_label.Text, LabelScale);
        (float glyphTop, float glyphBottom) = _font.MeasureVerticalBounds(_label.Text, LabelScale);

        _label.PixelPositionInCanvas = new Vector2(
            _position.X + ((_size.X - labelWidth) / 2.0F),
            _position.Y + ((_size.Y - (glyphBottom - glyphTop)) / 2.0F) - glyphTop);
    }

    private static UIImage AddPanel(UICanvas canvas, Vector3 color)
    {
        var panel = new UIImage(canvas, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = color,
        };

        canvas.AddComponentToRender(panel);
        return panel;
    }
}

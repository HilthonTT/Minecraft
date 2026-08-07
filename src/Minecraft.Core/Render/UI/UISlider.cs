using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI;

/// <summary>
/// A value dragged along a track, with its name and where it currently stands written across it.
/// <para>
/// Like <see cref="UIButton"/> this is not a component itself: it owns a few panels and a run of text on the
/// canvas it was given, and moves them together. A drag once begun stays with the slider it started on even
/// when the cursor wanders off it, which is what makes a slider draggable rather than only clickable.
/// </para>
/// </summary>
public sealed class UISlider
{
    public const int Height = 40;
    public const int Gap = 10;

    private const float LabelScale = 0.32F;

    /// <summary>How wide the handle is. Wide enough to grab, narrow enough to point with.</summary>
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

    /// <summary>Turns the current value into the text written on the slider, such as "8 chunks".</summary>
    private readonly Func<float, string> _describe;

    private Vector2 _position;
    private Vector2 _size;
    private float _value;

    /// <summary>Whether this slider is the one the mouse button went down on and has not yet come up from.</summary>
    private bool _isDragging;

    /// <summary>
    /// What the slider currently stands at. Setting it lays the handle out again but does not report a
    /// change, since the caller doing the setting is where the value came from.
    /// </summary>
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

        // Added in drawing order: the track, what is filled in of it, the handle, then the writing on top.
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

    /// <summary>
    /// Follows the mouse and reports the value if it moved this frame, or null if it did not. A slider is
    /// grabbed by pressing anywhere along it, which jumps the handle to the cursor and starts a drag, so a
    /// single click on a spot sets the value there as well.
    /// </summary>
    public float? Update(Vector2 mousePosition, bool mousePressed)
    {
        bool held = Game.Input.OnMouseDown(MouseButton.Left);

        if (mousePressed && Contains(mousePosition))
        {
            _isDragging = true;
        }
        else if (!held)
        {
            // Released, wherever the cursor had got to by then.
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

    /// <summary>
    /// The value the given horizontal position along the track stands for. The handle has a width of its own,
    /// so the travel it can be dragged over is that much shorter than the track it sits in.
    /// </summary>
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

        // Centred the same way a button centres its own label: the component's position is where the text box
        // starts rather than where its glyphs do, so the offset they hang by is taken back out.
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

using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI;

public sealed class UISlider : UIWidget
{
    public const float Height = UITheme.RowHeight;

    private const float KnobWidth = 10F;
    private const float LabelPadding = 10F;
    private const int PageSteps = 10;

    private readonly UIImage _outline;
    private readonly UIImage _track;
    private readonly UIImage _fill;
    private readonly UIImage _knobOutline;
    private readonly UIImage _knob;
    private readonly UILabel _label;

    private readonly HeldKey _decreaseKey = new(Keys.Left);
    private readonly HeldKey _increaseKey = new(Keys.Right);

    private readonly string _name;
    private readonly float _minimum;
    private readonly float _maximum;
    private readonly float _step;
    private readonly Func<float, string> _describe;

    private float _value;
    private float _hover;
    private bool _isDragging;

    public UISlider(UICanvas canvas, float minimum, float maximum, float step, Func<float, string> describe, string name = "")
        : base(canvas)
    {
        _name = name;
        _minimum = minimum;
        _maximum = Math.Max(minimum, maximum);
        _step = step;
        _describe = describe;
        _value = minimum;

        _outline = UIDraw.Rect(canvas, UITheme.Outline);
        _track = UIDraw.Rect(canvas, UITheme.Field);
        _fill = UIDraw.Rect(canvas, UITheme.AccentDim, 1F, UITextures.Gradient);
        _knobOutline = UIDraw.Rect(canvas, UITheme.Outline);
        _knob = UIDraw.Rect(canvas, UITheme.SurfaceHover, 1F, UITextures.Gradient);
        _label = new UILabel(canvas, UITheme.LabelScale, UITheme.Text);
    }

    public float Value
    {
        get => _value;
        set
        {
            _value = Snap(value, _minimum, _maximum, _step);
            Layout();
        }
    }

    public bool IsDragging => _isDragging;

    public override bool WantsHorizontalKeys => true;

    public override bool TryKeyboardActivate() => false;

    public static float Snap(float value, float minimum, float maximum, float step)
    {
        float clamped = Math.Clamp(value, minimum, maximum);
        if (step <= 0F)
        {
            return clamped;
        }

        float snapped = minimum + (MathF.Round((clamped - minimum) / step) * step);
        return Math.Clamp(snapped, minimum, maximum);
    }

    public static float ValueAt(float pixelX, float left, float width, float minimum, float maximum)
    {
        float travel = Math.Max(1F, width - KnobWidth);
        float fraction = Math.Clamp((pixelX - left - (KnobWidth / 2F)) / travel, 0F, 1F);
        return minimum + (fraction * (maximum - minimum));
    }

    public override bool Update(UIInput input)
    {
        bool hovered = IsEnabled && Contains(input.Mouse);
        float previous = _value;

        if (input.Pressed && hovered)
        {
            _isDragging = true;
        }
        else if (!input.Down)
        {
            _isDragging = false;
        }

        if (_isDragging)
        {
            _value = Snap(ValueAt(input.Mouse.X, Position.X, Size.X, _minimum, _maximum), _minimum, _maximum, _step);
        }

        if (IsFocused && IsEnabled)
        {
            UpdateKeys();
        }

        ConsumeActivation();

        _hover = UIMotion.Approach(_hover, hovered || _isDragging || FocusRingVisible ? 1F : 0F, UITheme.HoverSharpness);
        Paint();

        if (_value == previous)
        {
            return false;
        }

        Layout();
        return true;
    }

    protected override void OnLayout() => Layout();

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _outline.IsVisible = isVisible;
        _track.IsVisible = isVisible;
        _fill.IsVisible = isVisible;
        _knobOutline.IsVisible = isVisible;
        _knob.IsVisible = isVisible;
        _label.IsVisible = isVisible;
    }

    private void UpdateKeys()
    {
        float step = _step > 0F ? _step : (_maximum - _minimum) / 100F;

        if (_decreaseKey.HasFired())
        {
            _value = Snap(_value - step, _minimum, _maximum, _step);
        }

        if (_increaseKey.HasFired())
        {
            _value = Snap(_value + step, _minimum, _maximum, _step);
        }

        if (Game.Input.OnKeyPress(Keys.PageDown))
        {
            _value = Snap(_value - (step * PageSteps), _minimum, _maximum, _step);
        }

        if (Game.Input.OnKeyPress(Keys.PageUp))
        {
            _value = Snap(_value + (step * PageSteps), _minimum, _maximum, _step);
        }

        if (Game.Input.OnKeyPress(Keys.Home))
        {
            _value = _minimum;
        }

        if (Game.Input.OnKeyPress(Keys.End))
        {
            _value = _maximum;
        }
    }

    private void Paint()
    {
        _outline.Color = FocusRingVisible ? UITheme.Focus : UITheme.Outline;
        _fill.Color = Vector3.Lerp(UITheme.AccentDim, UITheme.Accent * 0.8F, _hover * 0.6F);
        _knob.Color = Vector3.Lerp(new Vector3(0.70F, 0.73F, 0.78F), UITheme.Text, _hover);
        _label.Color = IsEnabled ? UITheme.Text : UITheme.TextFaint;
    }

    private void Layout()
    {
        float border = UITheme.BorderWidth;
        Vector2 inner = Position + new Vector2(border, border);
        Vector2 innerSize = Size - new Vector2(2 * border, 2 * border);

        float fraction = _maximum > _minimum ? (_value - _minimum) / (_maximum - _minimum) : 0F;
        float travel = Math.Max(0F, innerSize.X - KnobWidth);
        float knobLeft = inner.X + (fraction * travel);

        UIDraw.Place(_outline, Position, Size);
        UIDraw.Place(_track, inner, innerSize);
        UIDraw.Place(_fill, inner, new Vector2((KnobWidth / 2F) + (fraction * travel), innerSize.Y));
        UIDraw.Place(_knobOutline, new Vector2(knobLeft - 1F, Position.Y), new Vector2(KnobWidth + 2F, Size.Y));
        UIDraw.Place(_knob, new Vector2(knobLeft, inner.Y), new Vector2(KnobWidth, innerSize.Y));

        string description = _describe(_value);
        string text = _name.Length == 0 ? description : _name + ": " + description;
        _label.SetFitted(text, Size.X - (2 * LabelPadding));
        _label.PlaceCentredIn(Position - new Vector2(0F, 1F), Size);
    }
}

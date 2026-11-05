using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI;

public sealed class UIToggle : UIWidget
{
    private const float TrackWidth = 38F;
    private const float TrackHeight = 18F;
    private const float KnobInset = 3F;
    private const float InnerPadding = 12F;

    private readonly UISurface _surface;
    private readonly UIImage _trackOutline;
    private readonly UIImage _track;
    private readonly UIImage _knob;
    private readonly UILabel _label;

    private readonly string _onText;
    private readonly string _offText;

    private bool _value;
    private float _knobPosition;
    private float _hover;
    private bool _armed;

    public UIToggle(UICanvas canvas, bool value, string onText = "On", string offText = "Off")
        : base(canvas)
    {
        _onText = onText;
        _offText = offText;
        _value = value;
        _knobPosition = value ? 1F : 0F;

        _surface = new UISurface(canvas);
        _trackOutline = UIDraw.Rect(canvas, UITheme.Outline);
        _track = UIDraw.Rect(canvas, UITheme.Field);
        _knob = UIDraw.Rect(canvas, UITheme.Text, 1F, UITextures.Gradient);
        _label = new UILabel(canvas, UITheme.LabelScale, UITheme.Text, value ? onText : offText);
    }

    public bool Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            _knobPosition = value ? 1F : 0F;
            RefreshLabel();
            LayoutKnob();
        }
    }

    public override bool WantsHorizontalKeys => true;

    public override bool Update(UIInput input)
    {
        bool hovered = IsEnabled && Contains(input.Mouse);
        bool? requested = null;

        if (input.Pressed && hovered)
        {
            _armed = true;
        }

        if (input.Released)
        {
            if (_armed && hovered)
            {
                requested = !_value;
            }

            _armed = false;
        }
        else if (!input.Down)
        {
            _armed = false;
        }

        if (ConsumeActivation())
        {
            requested = !_value;
        }

        if (IsFocused && IsEnabled)
        {
            if (Game.Input.OnKeyPress(Keys.Left))
            {
                requested = false;
            }
            else if (Game.Input.OnKeyPress(Keys.Right))
            {
                requested = true;
            }
        }

        bool changed = requested is bool next && next != _value;
        if (changed)
        {
            _value = requested!.Value;
            RefreshLabel();
        }

        _hover = UIMotion.Approach(_hover, hovered || FocusRingVisible ? 1F : 0F, UITheme.HoverSharpness);

        float target = _value ? 1F : 0F;
        if (_knobPosition != target)
        {
            _knobPosition = UIMotion.Approach(_knobPosition, target, UITheme.SlideSharpness);
            LayoutKnob();
        }

        Paint();
        return changed;
    }

    protected override void OnLayout()
    {
        _surface.Layout(Position, Size);

        Vector2 trackPosition = TrackPosition;
        UIDraw.Place(_trackOutline, trackPosition - new Vector2(2F, 2F), new Vector2(TrackWidth + 4F, TrackHeight + 4F));
        UIDraw.Place(_track, trackPosition, new Vector2(TrackWidth, TrackHeight));

        LayoutKnob();
        LayoutLabel();
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _surface.IsVisible = isVisible;
        _trackOutline.IsVisible = isVisible;
        _track.IsVisible = isVisible;
        _knob.IsVisible = isVisible;
        _label.IsVisible = isVisible;
    }

    private Vector2 TrackPosition => new(
        Position.X + Size.X - InnerPadding - TrackWidth,
        Position.Y + ((Size.Y - TrackHeight) / 2F));

    private void LayoutKnob()
    {
        float knobSize = TrackHeight - (2 * KnobInset);
        float travel = TrackWidth - (2 * KnobInset) - knobSize;
        Vector2 track = TrackPosition;

        UIDraw.Place(
            _knob,
            new Vector2(track.X + KnobInset + (travel * _knobPosition), track.Y + KnobInset),
            new Vector2(knobSize, knobSize));
    }

    private void RefreshLabel()
    {
        _label.Text = _value ? _onText : _offText;
        LayoutLabel();
    }

    private void LayoutLabel()
    {
        float available = Size.X - TrackWidth - (3 * InnerPadding);
        _label.PlaceMiddle(Position.X + InnerPadding + ((available - _label.Width) / 2F), Position.Y + (Size.Y / 2F) - 1F);
    }

    private void Paint()
    {
        if (!IsEnabled)
        {
            _surface.Paint(UITheme.SurfaceDisabled, UITheme.Outline, raised: false);
            _track.Color = UITheme.Field;
            _knob.Color = UITheme.TextFaint;
            _label.Color = UITheme.TextFaint;
            return;
        }

        _surface.Paint(UITheme.FillFor(UIButtonStyle.Normal, _hover, false), FocusRingVisible ? UITheme.Focus : UITheme.Outline);
        _track.Color = Vector3.Lerp(UITheme.Field, UITheme.Accent, _knobPosition);
        _knob.Color = UITheme.Text;
        _label.Color = _value ? UITheme.Text : UITheme.TextMuted;
    }
}

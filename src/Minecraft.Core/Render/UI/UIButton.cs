using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UIButton : UIWidget
{
    public const float Height = UITheme.ButtonHeight;
    public const float Gap = UITheme.Gap;

    private const float LabelScale = UITheme.LabelScale;
    private const float LabelPadding = 12F;
    private const float KeyboardFlashSeconds = 0.12F;

    private readonly UISurface _surface;
    private readonly UILabel _label;

    private string _text;
    private float _hover;
    private bool _armed;
    private bool _wasPressedLook;
    private double _flashUntil;

    public UIButton(UICanvas canvas, string text, UIButtonStyle style = UIButtonStyle.Normal)
        : base(canvas)
    {
        _text = text;
        Style = style;

        _surface = new UISurface(canvas);
        _label = new UILabel(canvas, LabelScale, UITheme.Text, text);
    }

    public UIButtonStyle Style { get; set; }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            LayoutLabel(_wasPressedLook);
        }
    }

    public static float MeasureRequiredWidth(string text)
    {
        return FontRegistry.GetFont(FontType.Arial).MeasureWidth(text, LabelScale) + (2 * LabelPadding);
    }

    public override bool Update(UIInput input)
    {
        bool hovered = IsEnabled && Contains(input.Mouse);
        bool clicked = false;

        if (input.Pressed && hovered)
        {
            _armed = true;
        }

        if (input.Released)
        {
            clicked = _armed && hovered;
            _armed = false;
        }
        else if (!input.Down)
        {
            _armed = false;
        }

        if (ConsumeActivation())
        {
            clicked = true;
            _flashUntil = UIMotion.NowSeconds + KeyboardFlashSeconds;
        }

        bool pressedLook = (_armed && hovered) || UIMotion.NowSeconds < _flashUntil;
        float hoverTarget = hovered || FocusRingVisible ? 1F : 0F;
        _hover = UIMotion.Approach(_hover, hoverTarget, UITheme.HoverSharpness);

        Paint(pressedLook);

        return clicked && IsEnabled && IsVisible;
    }

    protected override void OnLayout()
    {
        _surface.Layout(Position, Size);
        LayoutLabel(_wasPressedLook);
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _surface.IsVisible = isVisible;
        _label.IsVisible = isVisible;
    }

    private void Paint(bool pressedLook)
    {
        if (!IsEnabled)
        {
            _surface.Paint(UITheme.SurfaceDisabled, UITheme.Outline, raised: false);
            _label.Color = UITheme.TextFaint;
        }
        else
        {
            Vector3 outline = FocusRingVisible ? UITheme.Focus : UITheme.Outline;
            _surface.Paint(UITheme.FillFor(Style, _hover, pressedLook), outline, raised: !pressedLook);
            _label.Color = Vector3.Lerp(new Vector3(0.90F, 0.90F, 0.92F), UITheme.Text, _hover);
        }

        if (pressedLook != _wasPressedLook)
        {
            _wasPressedLook = pressedLook;
            LayoutLabel(pressedLook);
        }
    }

    private void LayoutLabel(bool pressed)
    {
        _label.SetFitted(_text, Size.X - (2 * LabelPadding));
        _label.PlaceCentredIn(Position + new Vector2(0F, pressed ? 1F : -1F), Size);
    }
}

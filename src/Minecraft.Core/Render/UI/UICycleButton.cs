using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI;

public sealed class UICycleButton : UIWidget
{
    private const float ArrowInset = 12F;

    private readonly UISurface _surface;
    private readonly UILabel _previousArrow;
    private readonly UILabel _nextArrow;
    private readonly UILabel _label;

    private readonly IReadOnlyList<string> _options;

    private int _selectedIndex;
    private float _hover;
    private bool _armed;

    public UICycleButton(UICanvas canvas, IReadOnlyList<string> options, int selectedIndex = 0)
        : base(canvas)
    {
        if (options.Count == 0)
        {
            throw new ArgumentException("A cycle button needs at least one option.", nameof(options));
        }

        _options = options;
        _selectedIndex = Math.Clamp(selectedIndex, 0, options.Count - 1);

        _surface = new UISurface(canvas);
        _previousArrow = new UILabel(canvas, UITheme.LabelScale, UITheme.TextMuted, "<");
        _nextArrow = new UILabel(canvas, UITheme.LabelScale, UITheme.TextMuted, ">");
        _label = new UILabel(canvas, UITheme.LabelScale, UITheme.Text, options[_selectedIndex]);
    }

    public IReadOnlyList<string> Options => _options;

    public string SelectedOption => _options[_selectedIndex];

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = Math.Clamp(value, 0, _options.Count - 1);
            if (clamped == _selectedIndex)
            {
                return;
            }

            _selectedIndex = clamped;
            LayoutLabel();
        }
    }

    public override bool WantsHorizontalKeys => true;

    public static int Cycle(int index, int count, int step)
    {
        return count <= 0 ? 0 : (((index + step) % count) + count) % count;
    }

    public override bool Update(UIInput input)
    {
        bool hovered = IsEnabled && Contains(input.Mouse);
        int step = 0;

        if (input.Pressed && hovered)
        {
            _armed = true;
        }

        if (input.Released)
        {
            if (_armed && hovered)
            {
                step = input.Mouse.X < Position.X + (Size.X * 0.25F) ? -1 : 1;
            }

            _armed = false;
        }
        else if (!input.Down)
        {
            _armed = false;
        }

        if (input.RightPressed && hovered)
        {
            step = -1;
        }

        if (ConsumeActivation())
        {
            step = 1;
        }

        if (IsFocused && IsEnabled)
        {
            if (Game.Input.OnKeyPress(Keys.Left))
            {
                step = -1;
            }
            else if (Game.Input.OnKeyPress(Keys.Right))
            {
                step = 1;
            }
        }

        bool changed = false;
        if (step != 0 && _options.Count > 1)
        {
            _selectedIndex = Cycle(_selectedIndex, _options.Count, step);
            LayoutLabel();
            changed = true;
        }

        _hover = UIMotion.Approach(_hover, hovered || FocusRingVisible ? 1F : 0F, UITheme.HoverSharpness);
        Paint(_armed && hovered);

        return changed;
    }

    protected override void OnLayout()
    {
        _surface.Layout(Position, Size);

        float middle = Position.Y + (Size.Y / 2F) - 1F;
        _previousArrow.PlaceMiddle(Position.X + ArrowInset, middle);
        _nextArrow.PlaceMiddle(Position.X + Size.X - ArrowInset - _nextArrow.Width, middle);

        LayoutLabel();
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _surface.IsVisible = isVisible;
        _previousArrow.IsVisible = isVisible;
        _nextArrow.IsVisible = isVisible;
        _label.IsVisible = isVisible;
    }

    private void LayoutLabel()
    {
        float arrowsWidth = 2 * (ArrowInset + _nextArrow.Width + 6F);
        _label.SetFitted(_options[_selectedIndex], Size.X - arrowsWidth);
        _label.PlaceCentredIn(Position - new Vector2(0F, 1F), Size);
    }

    private void Paint(bool pressed)
    {
        if (!IsEnabled)
        {
            _surface.Paint(UITheme.SurfaceDisabled, UITheme.Outline, raised: false);
            _label.Color = UITheme.TextFaint;
            _previousArrow.Color = UITheme.TextFaint;
            _nextArrow.Color = UITheme.TextFaint;
            return;
        }

        Vector3 outline = FocusRingVisible ? UITheme.Focus : UITheme.Outline;
        _surface.Paint(UITheme.FillFor(UIButtonStyle.Normal, _hover, pressed), outline, raised: !pressed);
        _label.Color = UITheme.Text;

        Vector3 arrow = Vector3.Lerp(UITheme.TextFaint, UITheme.Accent, _hover);
        _previousArrow.Color = arrow;
        _nextArrow.Color = arrow;
    }
}

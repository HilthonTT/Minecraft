using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public abstract class UIWidget
{
    private bool _isVisible = true;
    private bool _activationRequested;

    protected UIWidget(UICanvas canvas)
    {
        Canvas = canvas;
    }

    public static bool ShowFocusRing { get; set; }

    public UICanvas Canvas { get; }

    public Vector2 Position { get; private set; }

    public Vector2 Size { get; private set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsFocused { get; set; }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnVisibilityChanged(value);
        }
    }

    public virtual bool CanFocus => IsVisible && IsEnabled;

    public virtual bool WantsHorizontalKeys => false;

    public virtual bool WantsVerticalKeys => false;

    public virtual bool WantsText => false;

    protected bool FocusRingVisible => IsFocused && ShowFocusRing && IsEnabled;

    public void SetBounds(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        OnLayout();
    }

    public void SetBounds(float left, float top, float width, float height)
    {
        SetBounds(new Vector2(left, top), new Vector2(width, height));
    }

    public bool Contains(Vector2 point)
    {
        return IsVisible &&
               point.X >= Position.X && point.X <= Position.X + Size.X &&
               point.Y >= Position.Y && point.Y <= Position.Y + Size.Y;
    }

    public virtual bool TryKeyboardActivate()
    {
        if (!IsEnabled || !IsVisible)
        {
            return false;
        }

        _activationRequested = true;
        return true;
    }

    public abstract bool Update(UIInput input);

    protected bool ConsumeActivation()
    {
        bool requested = _activationRequested;
        _activationRequested = false;
        return requested && IsEnabled && IsVisible;
    }

    protected abstract void OnLayout();

    protected abstract void OnVisibilityChanged(bool isVisible);
}

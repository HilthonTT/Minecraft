using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI.Presets;

public abstract class UICanvasMenu : UICanvas
{
    private const float SubtitleGap = 10F;
    private const float TitleToCardGap = 22F;
    private const float StatusGap = 14F;
    private const float MinimumTop = 16F;

    private const float SceneDimAtTitle = 0.48F;
    private const float VignetteAtTitle = 0.75F;

    private readonly UIImage _scene;
    private readonly UIImage _dim;
    private readonly UIImage _vignette;
    private readonly UIPanel _card;
    private readonly UILabel _title;
    private readonly UILabel _subtitle;
    private readonly UILabel _status;
    private readonly UILabel _hints;

    private readonly List<UIWidget> _focusOrder = [];

    private float _statusTop;
    private float _statusCentre;
    private bool _atTitle = true;

    protected UICanvasMenu(Game game, string title)
        : base(Vector3.Zero, Vector3.Zero, UIScale.CanvasWidth, UIScale.CanvasHeight, RenderSpace.Screen)
    {
        Game = game;
        Font = FontRegistry.GetFont(FontType.Arial);

        IsEnabled = false;

        _scene = UIDraw.Rect(this, Vector3.One, 1F, UITextures.Scene);
        _dim = UIDraw.Rect(this, UITheme.Dim, UITheme.DimTransparency);
        _vignette = UIDraw.Rect(this, Vector3.One, VignetteAtTitle, UITextures.Vignette);
        _card = new UIPanel(this);

        _title = new UILabel(this, TitleScale, UITheme.Text, title);
        _subtitle = new UILabel(this, UITheme.BodyScale, UITheme.TextMuted);
        _status = new UILabel(this, UITheme.BodyScale, UITheme.TextMuted);
        _hints = new UILabel(this, UITheme.TinyScale, UITheme.TextFaint, string.Empty, shadow: false);
    }

    protected Game Game { get; }

    protected Font Font { get; }

    protected virtual float TitleScale => UITheme.TitleScale;

    protected virtual float MaxRowWidth => 420;

    protected virtual bool ShowsCard => true;

    protected virtual string KeyHints => "Esc  Back      Enter  Select      Tab / Arrows  Move";

    protected float RowWidth => Math.Min(MaxRowWidth, Math.Max(1, PixelWidth - (2 * (UITheme.Margin + UITheme.Padding))));

    protected float RowLeft => (PixelWidth - RowWidth) / 2.0F;

    protected UIWidget? Primary { get; set; }

    protected UIWidget? FocusedWidget { get; private set; }

    protected UILabel TitleLabel => _title;

    public string Title => _title.Text;

    public void SetTitle(string title)
    {
        if (_title.Text == title)
        {
            return;
        }

        _title.Text = title;
        Layout();
    }

    public void SetSubtitle(string subtitle, Vector3? color = null)
    {
        _subtitle.Color = color ?? UITheme.TextMuted;

        if (_subtitle.Text == subtitle)
        {
            return;
        }

        _subtitle.Text = subtitle;
        Layout();
    }

    public void SetStatus(string text, bool isError = false)
    {
        _status.Color = isError ? UITheme.Error : UITheme.Success;
        _status.SetWrapped(text, Math.Max(1F, PixelWidth - (2 * UITheme.Margin)));
        LayoutStatus();
    }

    public void ClearStatus() => SetStatus(string.Empty);

    public virtual void OnShown()
    {
        ClearStatus();

        bool atTitle = Game.State == GameState.MainMenu;
        if (atTitle != _atTitle)
        {
            _atTitle = atTitle;
            LayoutBackdrop();
        }

        UIWidget? initial = InitialFocus ?? (UIWidget.ShowFocusRing ? Primary : null);
        Focus(initial is { CanFocus: true } ? initial : null);
    }

    protected virtual UIWidget? InitialFocus => null;

    public MenuAction HandleInput(UIInput input)
    {
        if (FocusedWidget is { CanFocus: false })
        {
            Focus(null);
        }

        FocusFromMouse(input);
        HandleNavigationKeys();

        return Update(input);
    }

    protected abstract MenuAction Update(UIInput input);

    protected abstract void Layout();

    protected void SetFocusOrder(params UIWidget[] widgets)
    {
        _focusOrder.Clear();
        _focusOrder.AddRange(widgets);
    }

    protected void Focus(UIWidget? widget)
    {
        foreach (UIWidget candidate in _focusOrder)
        {
            candidate.IsFocused = candidate == widget;
        }

        if (widget is not null && !_focusOrder.Contains(widget))
        {
            widget.IsFocused = true;
        }

        FocusedWidget = widget;
        OnFocusChanged(widget);
    }

    protected virtual void OnFocusChanged(UIWidget? widget)
    {
    }

    protected void MoveFocus(int direction)
    {
        int current = FocusedWidget is null ? -1 : _focusOrder.IndexOf(FocusedWidget);
        int next = UIFocus.Step(current, _focusOrder.Count, direction, index => _focusOrder[index].CanFocus);

        if (next >= 0)
        {
            UIWidget.ShowFocusRing = true;
            Focus(_focusOrder[next]);
        }
    }

    protected float CenteredLeft(float width) => (PixelWidth - width) / 2.0F;

    protected float TitleBlockHeight
    {
        get
        {
            float height = _title.CapHeight + TitleToCardGap;
            if (_subtitle.Text.Length > 0)
            {
                height += _subtitle.CapHeight + SubtitleGap;
            }

            return height;
        }
    }

    protected float ContentTopFor(float contentHeight)
    {
        float padding = ShowsCard ? UITheme.Padding : 0F;
        float block = TitleBlockHeight + contentHeight + (2 * padding);
        float top = (PixelHeight - block) / 2F;
        return Math.Max(MinimumTop, top) + TitleBlockHeight + padding;
    }

    protected void LayoutFrame(float contentTop, float contentBottom)
    {
        LayoutBackdrop();

        float padding = ShowsCard ? UITheme.Padding : 0F;
        float cardTop = contentTop - padding;

        _card.IsVisible = ShowsCard;
        if (ShowsCard)
        {
            _card.SetBounds(
                RowLeft - padding,
                cardTop,
                RowWidth + (2 * padding),
                contentBottom - contentTop + (2 * padding));
        }

        float titleBottom = cardTop - TitleToCardGap;
        if (_subtitle.Text.Length > 0)
        {
            _subtitle.IsVisible = true;
            _subtitle.PlaceTop(CenteredLeft(_subtitle.Width), titleBottom - _subtitle.CapHeight);
            titleBottom -= _subtitle.CapHeight + SubtitleGap;
        }
        else
        {
            _subtitle.IsVisible = false;
        }

        _title.PlaceTop(CenteredLeft(_title.Width), Math.Max(MinimumTop / 2F, titleBottom - _title.CapHeight));

        _statusTop = contentBottom + padding + StatusGap;
        _statusCentre = PixelWidth / 2F;
        LayoutStatus();

        _hints.Text = KeyHints;
        _hints.PlaceTop(UITheme.Margin, PixelHeight - UITheme.Margin - _hints.CapHeight);
    }

    protected override void OnDimensionsChanged() => Layout();

    private void LayoutBackdrop()
    {
        UIDraw.Place(_scene, ScenePosition(), SceneSize());
        UIDraw.Place(_dim, Vector2.Zero, new Vector2(PixelWidth, PixelHeight));
        UIDraw.Place(_vignette, Vector2.Zero, new Vector2(PixelWidth, PixelHeight));

        _scene.IsVisible = _atTitle;
        _dim.Transparency = _atTitle ? SceneDim : UITheme.DimTransparency;
        _vignette.Transparency = _atTitle ? VignetteAtTitle : 0.6F;
    }

    protected virtual float SceneDim => SceneDimAtTitle;

    private Vector2 SceneSize()
    {
        float aspect = UIPixelArt.SceneWidth / (float)UIPixelArt.SceneHeight;
        float width = Math.Max(PixelWidth, PixelHeight * aspect);
        return new Vector2(width, width / aspect);
    }

    private Vector2 ScenePosition()
    {
        Vector2 size = SceneSize();
        return new Vector2((PixelWidth - size.X) / 2F, PixelHeight - size.Y);
    }

    private void LayoutStatus()
    {
        float widest = 0F;
        foreach (string line in _status.Text.Split('\n'))
        {
            widest = Math.Max(widest, _status.Measure(line));
        }

        _status.PlaceTop(_statusCentre - (widest / 2F), _statusTop);
    }

    private void FocusFromMouse(UIInput input)
    {
        if (!input.Pressed)
        {
            return;
        }

        foreach (UIWidget widget in _focusOrder)
        {
            if (widget.CanFocus && widget.Contains(input.Mouse))
            {
                UIWidget.ShowFocusRing = false;
                Focus(widget);
                return;
            }
        }
    }

    private void HandleNavigationKeys()
    {
        UIWidget? focused = FocusedWidget;

        if (UIInput.KeyPressed(Keys.Tab))
        {
            MoveFocus(UIInput.ShiftDown ? -1 : 1);
            return;
        }

        if (focused is null || !focused.WantsVerticalKeys)
        {
            if (UIInput.KeyPressed(Keys.Down))
            {
                MoveFocus(1);
                return;
            }

            if (UIInput.KeyPressed(Keys.Up))
            {
                MoveFocus(-1);
                return;
            }
        }

        if (focused is not null && !focused.WantsHorizontalKeys)
        {
            if (UIInput.KeyPressed(Keys.Right))
            {
                MoveFocus(1);
                return;
            }

            if (UIInput.KeyPressed(Keys.Left))
            {
                MoveFocus(-1);
                return;
            }
        }

        if (UIInput.EnterPressed)
        {
            if (focused is null || !focused.TryKeyboardActivate())
            {
                Primary?.TryKeyboardActivate();
            }

            return;
        }

        if (UIInput.KeyPressed(Keys.Space) && focused is { WantsText: false })
        {
            focused.TryKeyboardActivate();
        }
    }
}

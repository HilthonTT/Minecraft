using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public abstract class UICanvasMenu : UICanvas
{
    private const float TitleScale = 0.7F;
    private const float StatusScale = 0.32F;

    private const int SideMarginPixels = 40;

    private static readonly Vector3 _titleColor = new(0.95F, 0.95F, 0.95F);
    private static readonly Vector3 _statusColor = new(0.75F, 0.75F, 0.78F);
    private static readonly Vector3 _errorColor = new(1.0F, 0.45F, 0.40F);

    private readonly UIImage _backdrop;
    private readonly UIText _title;
    private readonly UIText _status;

    private float _statusTop;

    protected Font Font { get; }

    protected virtual float MaxRowWidth => 400;

    protected float RowWidth => Math.Min(MaxRowWidth, Math.Max(1, PixelWidth - (2 * SideMarginPixels)));

    protected float RowLeft => (PixelWidth - RowWidth) / 2.0F;

    protected UICanvasMenu(Game game, string title, Vector3 backdropColor, float backdropTransparency)
        : base(
            Vector3.Zero,
            Vector3.Zero,
            game.Window.ClientSize.X,
            game.Window.ClientSize.Y,
            RenderSpace.Screen)
    {
        Font = FontRegistry.GetFont(FontType.Arial);

        IsEnabled = false;

        _backdrop = new UIImage(this, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = backdropColor,
            Transparency = backdropTransparency,
        };
        AddComponentToRender(_backdrop);

        _title = new UIText(this, Font, Vector2.Zero, new Vector2(TitleScale, TitleScale), title)
        {
            Color = _titleColor,
        };
        AddComponentToRender(_title);

        _status = new UIText(this, Font, Vector2.Zero, new Vector2(StatusScale, StatusScale), string.Empty)
        {
            Color = _statusColor,
        };
        AddComponentToRender(_status);
    }

    public void SetStatus(string text, bool isError = false)
    {
        _status.Text = text;
        _status.Color = isError ? _errorColor : _statusColor;
        LayoutStatus();
    }

    public void ClearStatus() => SetStatus(string.Empty);

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

    public virtual void OnShown() => ClearStatus();

    public abstract MenuAction HandleInput(Vector2 mousePosition, bool mousePressed);

    protected float CenteredTextLeft(string text, float scale) => (PixelWidth - Font.MeasureWidth(text, scale)) / 2.0F;

    protected void LayoutFrame(float contentTop, float statusTop)
    {
        const int titleToContentGap = 30;

        _backdrop.PixelPositionInCanvas = Vector2.Zero;
        _backdrop.Dimension = new Vector2(PixelWidth, PixelHeight);

        (_, float titleBottom) = Font.MeasureVerticalBounds(_title.Text, TitleScale);
        _title.PixelPositionInCanvas = new Vector2(
            CenteredTextLeft(_title.Text, TitleScale),
            Math.Max(10, contentTop - titleToContentGap - titleBottom));

        _statusTop = statusTop;
        LayoutStatus();
    }

    private void LayoutStatus()
    {
        (float top, _) = Font.MeasureVerticalBounds(_status.Text, StatusScale);
        _status.PixelPositionInCanvas = new Vector2(CenteredTextLeft(_status.Text, StatusScale), _statusTop - top);
    }

    protected override void OnDimensionsChanged() => Layout();

    protected abstract void Layout();
}

using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// What every full screen menu is made of: a backdrop covering the canvas, a title above the content and a
/// status line under it. Screens fill in their own rows and decide what a click on one of them means.
/// </summary>
public abstract class UICanvasMenu : UICanvas
{
    private const float TitleScale = 0.7F;
    private const float StatusScale = 0.32F;

    /// <summary>How wide a row of the menu is at most, and how much space is left either side of one.</summary>
    private const int MaxRowWidth = 400;
    private const int SideMarginPixels = 40;

    private static readonly Vector3 _titleColor = new(0.95F, 0.95F, 0.95F);
    private static readonly Vector3 _statusColor = new(0.75F, 0.75F, 0.78F);
    private static readonly Vector3 _errorColor = new(1.0F, 0.45F, 0.40F);

    private readonly UIImage _backdrop;
    private readonly UIText _title;
    private readonly UIText _status;

    /// <summary>Where the status line's glyphs should start, kept so a new message can be recentred.</summary>
    private float _statusTop;

    protected Font Font { get; }

    /// <summary>How wide a row of the menu is, kept inside the canvas on a narrow window.</summary>
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

        // Screens are built once and left registered with the renderer, so a canvas starts switched off and
        // is only enabled while its screen is the one being shown.
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

    /// <summary>Shows a line under the menu, used to say why something the player asked for did not happen.</summary>
    public void SetStatus(string text, bool isError = false)
    {
        _status.Text = text;
        _status.Color = isError ? _errorColor : _statusColor;
        LayoutStatus();
    }

    public void ClearStatus() => SetStatus(string.Empty);

    /// <summary>Renames the screen, for one that is reached from more than one place.</summary>
    public void SetTitle(string title)
    {
        if (_title.Text == title)
        {
            return;
        }

        _title.Text = title;
        Layout();
    }

    /// <summary>Called when the screen is opened, so it can start from a clean state.</summary>
    public virtual void OnShown() => ClearStatus();

    /// <summary>Handles a frame of input and reports what the player asked the game to do.</summary>
    public abstract MenuAction HandleInput(Vector2 mousePosition, bool mousePressed);

    /// <summary>The left edge a run of text needs to end up centred in the canvas.</summary>
    protected float CenteredTextLeft(string text, float scale) => (PixelWidth - Font.MeasureWidth(text, scale)) / 2.0F;

    /// <summary>
    /// Places the backdrop, the title and the status line around the content a screen has laid out. Both
    /// are positioned by where their glyphs land rather than by where their text components start, since a
    /// glyph hangs below its component by an offset of its own.
    /// </summary>
    /// <param name="contentTop">Where the screen's own rows begin, which the title sits above.</param>
    /// <param name="statusTop">Where the status line should appear, below the screen's last row.</param>
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

    /// <summary>Lays the screen out for the canvas size it currently has.</summary>
    protected abstract void Layout();
}

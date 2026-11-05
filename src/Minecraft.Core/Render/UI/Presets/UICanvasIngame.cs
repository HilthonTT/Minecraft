using System.Globalization;
using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasIngame : UICanvas
{
    private const float CrosshairArm = 9F;
    private const float CrosshairThickness = 2F;
    private const float CrosshairOutline = 1F;
    private const float CrosshairTransparency = 0.9F;

    private const double FpsRefreshSeconds = 0.25;

    private readonly Game _game;

    private readonly UIImage _crosshairShadowHorizontal;
    private readonly UIImage _crosshairShadowVertical;
    private readonly UIImage _crosshairHorizontal;
    private readonly UIImage _crosshairVertical;

    private readonly UILabel _fps;
    private readonly UIChat _chat;
    private readonly UIToast _toast;

    private double _fpsRefreshedAt;

    public UICanvasIngame(Game game)
        : base(Vector3.Zero, Vector3.Zero, UIScale.CanvasWidth, UIScale.CanvasHeight, RenderSpace.Screen)
    {
        _game = game;

        _crosshairShadowHorizontal = UIDraw.Rect(this, UITheme.Outline, 0.55F);
        _crosshairShadowVertical = UIDraw.Rect(this, UITheme.Outline, 0.55F);
        _crosshairHorizontal = UIDraw.Rect(this, Vector3.One, CrosshairTransparency);
        _crosshairVertical = UIDraw.Rect(this, Vector3.One, CrosshairTransparency);

        _fps = new UILabel(this, UITheme.SmallScale, UITheme.Text) { IsVisible = false };

        _chat = new UIChat(game, this);
        _toast = new UIToast(this);

        LayoutCrosshair();
    }

    public bool IsTyping => _chat.IsTyping;

    public void AddUserMessage(string sender, string message) => _chat.AddUserMessage(sender, message);

    public void AddSystemMessage(string message) => _chat.AddSystemMessage(message);

    public void ShowToast(string title, string detail = "") => _toast.Show(title, detail, UITheme.Accent);

    public void ShowWarningToast(string title, string detail = "") => _toast.Show(title, detail, UITheme.Warning);

    public void OnWorldUnloaded()
    {
        _chat.Clear();
        _toast.Hide();
    }

    public override void Update()
    {
        bool showCrosshair = _game.State == GameState.Playing && !_chat.IsTyping;
        _crosshairShadowHorizontal.IsVisible = showCrosshair;
        _crosshairShadowVertical.IsVisible = showCrosshair;
        _crosshairHorizontal.IsVisible = showCrosshair;
        _crosshairVertical.IsVisible = showCrosshair;

        UpdateFps();

        _chat.Update();
        _toast.Update();
    }

    protected override void OnDimensionsChanged()
    {
        LayoutCrosshair();
        _chat.OnCanvasResized();
        _toast.Relayout();
        _fpsRefreshedAt = 0;
    }

    private void UpdateFps()
    {
        _fps.IsVisible = _game.Settings.ShowFps;
        if (!_fps.IsVisible || UIMotion.NowSeconds - _fpsRefreshedAt < FpsRefreshSeconds)
        {
            return;
        }

        _fpsRefreshedAt = UIMotion.NowSeconds;
        _fps.Text = MathF.Round(_game.CurrentFPS).ToString("0", CultureInfo.InvariantCulture) + " FPS";
        _fps.PlaceTop(PixelWidth - UITheme.Margin - _fps.Width, UITheme.Margin);
    }

    private void LayoutCrosshair()
    {
        Vector2 centre = new(MathF.Round(PixelWidth / 2F), MathF.Round(PixelHeight / 2F));
        float length = (2 * CrosshairArm) + CrosshairThickness;
        float half = length / 2F;
        float thick = CrosshairThickness / 2F;
        float outline = CrosshairOutline;

        UIDraw.Place(
            _crosshairShadowHorizontal,
            centre.X - half - outline,
            centre.Y - thick - outline,
            length + (2 * outline),
            CrosshairThickness + (2 * outline));
        UIDraw.Place(
            _crosshairShadowVertical,
            centre.X - thick - outline,
            centre.Y - half - outline,
            CrosshairThickness + (2 * outline),
            length + (2 * outline));

        UIDraw.Place(_crosshairHorizontal, centre.X - half, centre.Y - thick, length, CrosshairThickness);
        UIDraw.Place(_crosshairVertical, centre.X - thick, centre.Y - half, CrosshairThickness, length);
    }
}

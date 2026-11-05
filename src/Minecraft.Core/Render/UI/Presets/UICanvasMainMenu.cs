using Minecraft.Core.Games;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasMainMenu : UICanvasMenu
{
    private const float LogoScale = 1.25F;
    private const float ExtraLogoGap = 22F;

    private readonly UIButton _singleplayerButton;
    private readonly UIButton _multiplayerButton;
    private readonly UIButton _optionsButton;
    private readonly UIButton _quitButton;
    private readonly UILabel _version;

    public UICanvasMainMenu(Game game)
        : base(game, "MINECRAFT")
    {
        _singleplayerButton = new UIButton(this, "Singleplayer", UIButtonStyle.Primary);
        _multiplayerButton = new UIButton(this, "Multiplayer");
        _optionsButton = new UIButton(this, "Options");
        _quitButton = new UIButton(this, "Quit Game");

        _version = new UILabel(this, UITheme.TinyScale, UITheme.TextFaint, GameVersion.DisplayName, shadow: false);

        SetFocusOrder(_singleplayerButton, _multiplayerButton, _optionsButton, _quitButton);
        Primary = _singleplayerButton;

        SetSubtitle("OpenGL Edition", UITheme.Warning);
    }

    protected override float TitleScale => LogoScale;

    protected override float MaxRowWidth => 380;

    protected override bool ShowsCard => false;

    protected override float SceneDim => 0.22F;

    protected override string KeyHints => "Enter  Select      Tab / Arrows  Move";

    protected override MenuAction Update(UIInput input)
    {
        if (_singleplayerButton.Update(input))
        {
            return MenuAction.Singleplayer;
        }

        if (_multiplayerButton.Update(input))
        {
            return MenuAction.Multiplayer;
        }

        if (_optionsButton.Update(input))
        {
            return MenuAction.Options;
        }

        return _quitButton.Update(input) ? MenuAction.QuitGame : MenuAction.None;
    }

    protected override void Layout()
    {
        float rowHeight = UIButton.Height + 4F;
        float gap = UITheme.RowGap;
        float columnHeight = ExtraLogoGap + (3 * rowHeight) + (2 * gap);

        float contentTop = ContentTopFor(columnHeight);
        float top = contentTop + ExtraLogoGap;
        var wide = new Vector2(RowWidth, rowHeight);

        _singleplayerButton.SetBounds(new Vector2(RowLeft, top), wide);
        top += rowHeight + gap;

        _multiplayerButton.SetBounds(new Vector2(RowLeft, top), wide);
        top += rowHeight + gap;

        float half = (RowWidth - gap) / 2F;
        _optionsButton.SetBounds(new Vector2(RowLeft, top), new Vector2(half, rowHeight));
        _quitButton.SetBounds(new Vector2(RowLeft + half + gap, top), new Vector2(half, rowHeight));
        top += rowHeight;

        LayoutFrame(contentTop, top);

        _version.PlaceTop(
            PixelWidth - UITheme.Margin - _version.Width,
            PixelHeight - UITheme.Margin - _version.CapHeight);
    }
}

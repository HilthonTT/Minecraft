using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasPauseMenu : UICanvasMenu
{
    private readonly UIButton _resumeButton;
    private readonly UIButton _optionsButton;
    private readonly UIButton _quitToTitleButton;
    private readonly UIButton _quitGameButton;

    public UICanvasPauseMenu(Game game)
        : base(game, "Game Paused")
    {
        _resumeButton = new UIButton(this, "Back to Game", UIButtonStyle.Primary);
        _optionsButton = new UIButton(this, "Options...");
        _quitToTitleButton = new UIButton(this, "Save and Quit to Title");
        _quitGameButton = new UIButton(this, "Save and Quit Game");

        SetFocusOrder(_resumeButton, _optionsButton, _quitToTitleButton, _quitGameButton);
        Primary = _resumeButton;

        Layout();
    }

    protected override float MaxRowWidth => 360;

    protected override string KeyHints => "Esc  Back to game      Enter  Select      Tab / Arrows  Move";

    public override void OnShown()
    {
        _quitToTitleButton.Text = Game.IsServer ? "Save and Quit to Title" : "Disconnect";
        _quitGameButton.Text = Game.IsServer ? "Save and Quit Game" : "Quit Game";
        SetSubtitle(Game.IsServer ? Game.WorldName : "Playing on a server");

        base.OnShown();
    }

    protected override MenuAction Update(UIInput input)
    {
        if (_resumeButton.Update(input))
        {
            return MenuAction.Resume;
        }

        if (_optionsButton.Update(input))
        {
            return MenuAction.Options;
        }

        if (_quitToTitleButton.Update(input))
        {
            return MenuAction.QuitToTitle;
        }

        return _quitGameButton.Update(input) ? MenuAction.QuitGame : MenuAction.None;
    }

    protected override void Layout()
    {
        const float separation = 14F;

        float gap = UITheme.RowGap;
        float columnHeight = (4 * UIButton.Height) + (2 * gap) + separation + gap;

        float top = ContentTopFor(columnHeight);
        float contentTop = top;
        var row = new Vector2(RowWidth, UIButton.Height);

        _resumeButton.SetBounds(new Vector2(RowLeft, top), row);
        top += UIButton.Height + gap;

        _optionsButton.SetBounds(new Vector2(RowLeft, top), row);
        top += UIButton.Height + gap + separation;

        _quitToTitleButton.SetBounds(new Vector2(RowLeft, top), row);
        top += UIButton.Height + gap;

        _quitGameButton.SetBounds(new Vector2(RowLeft, top), row);
        top += UIButton.Height;

        LayoutFrame(contentTop, top);
    }
}

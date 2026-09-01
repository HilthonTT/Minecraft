using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasMainMenu : UICanvasMenu
{
    private static readonly Vector3 _backdropColor = new(0.06F, 0.07F, 0.09F);

    private readonly UIButton _singleplayerButton;
    private readonly UIButton _multiplayerButton;
    private readonly UIButton _optionsButton;
    private readonly UIButton _quitButton;

    public UICanvasMainMenu(Game game)
        : base(game, "Minecraft OpenGL", _backdropColor, 1.0F)
    {
        _singleplayerButton = new UIButton(this, "Singleplayer");
        _multiplayerButton = new UIButton(this, "Multiplayer");
        _optionsButton = new UIButton(this, "Options");
        _quitButton = new UIButton(this, "Quit Game");

        Layout();
    }

    public override MenuAction HandleInput(Vector2 mousePosition, bool mousePressed)
    {
        if (_singleplayerButton.Update(mousePosition, mousePressed))
        {
            return MenuAction.Singleplayer;
        }

        if (_multiplayerButton.Update(mousePosition, mousePressed))
        {
            return MenuAction.Multiplayer;
        }

        if (_optionsButton.Update(mousePosition, mousePressed))
        {
            return MenuAction.Options;
        }

        if (_quitButton.Update(mousePosition, mousePressed))
        {
            return MenuAction.QuitGame;
        }

        return MenuAction.None;
    }

    protected override void Layout()
    {
        const int rowCount = 4;
        float columnHeight = (rowCount * UIButton.Height) + ((rowCount - 1) * UIButton.Gap);

        float columnTop = Math.Max(110, (PixelHeight - columnHeight) / 2.0F) + 20;
        var rowSize = new Vector2(RowWidth, UIButton.Height);

        float row = columnTop;
        foreach (UIButton button in (UIButton[])[_singleplayerButton, _multiplayerButton, _optionsButton, _quitButton])
        {
            button.SetBounds(new Vector2(RowLeft, row), rowSize);
            row += UIButton.Height + UIButton.Gap;
        }

        LayoutFrame(columnTop, columnTop + columnHeight + 24);
    }
}

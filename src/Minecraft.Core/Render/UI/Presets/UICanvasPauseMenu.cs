using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// What Escape opens while a world is loaded. The world is still drawn behind it, so the backdrop only dims
/// it rather than covering it.
/// </summary>
public sealed class UICanvasPauseMenu : UICanvasMenu
{
    private const float BackdropTransparency = 0.65F;

    private static readonly Vector3 _backdropColor = new(0.02F, 0.02F, 0.03F);

    private readonly UIButton _resumeButton;
    private readonly UIButton _optionsButton;
    private readonly UIButton _quitToTitleButton;
    private readonly UIButton _quitGameButton;

    public UICanvasPauseMenu(Game game)
        : base(game, "Game Menu", _backdropColor, BackdropTransparency)
    {
        _resumeButton = new UIButton(this, "Back to Game");
        _optionsButton = new UIButton(this, "Options");
        _quitToTitleButton = new UIButton(this, "Save and Quit to Title");
        _quitGameButton = new UIButton(this, "Quit Game");

        Layout();
    }

    public override MenuAction HandleInput(Vector2 mousePosition, bool mousePressed)
    {
        if (_resumeButton.Update(mousePosition, mousePressed))
        {
            return MenuAction.Resume;
        }

        if (_optionsButton.Update(mousePosition, mousePressed))
        {
            return MenuAction.Options;
        }

        if (_quitToTitleButton.Update(mousePosition, mousePressed))
        {
            return MenuAction.QuitToTitle;
        }

        if (_quitGameButton.Update(mousePosition, mousePressed))
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
        foreach (UIButton button in (UIButton[])[_resumeButton, _optionsButton, _quitToTitleButton, _quitGameButton])
        {
            button.SetBounds(new Vector2(RowLeft, row), rowSize);
            row += UIButton.Height + UIButton.Gap;
        }

        LayoutFrame(columnTop, columnTop + columnHeight + 24);
    }
}

using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// Asks before a world is deleted. Deleting one cannot be taken back and there is nothing to restore it
/// from, so it gets a screen of its own that names the world rather than a button that acts on one press.
/// </summary>
public sealed class UICanvasDeleteWorld : UICanvasMenu
{
    private const float MessageScale = 0.34F;
    private const float WarningScale = 0.30F;
    private const int LineGap = 8;
    private const int SectionGap = 24;

    private static readonly Vector3 _backdropColor = new(0.06F, 0.07F, 0.09F);
    private static readonly Vector3 _messageColor = new(0.90F, 0.90F, 0.92F);
    private static readonly Vector3 _warningColor = new(1.0F, 0.55F, 0.50F);

    private readonly UIText _message;
    private readonly UIText _warning;
    private readonly UIButton _cancelButton;
    private readonly UIButton _deleteButton;

    /// <summary>The world this screen was opened for.</summary>
    public string WorldName { get; private set; } = string.Empty;

    public UICanvasDeleteWorld(Game game)
        : base(game, "Delete World", _backdropColor, 1.0F)
    {
        _message = new UIText(this, Font, Vector2.Zero, new Vector2(MessageScale, MessageScale), string.Empty)
        {
            Color = _messageColor,
        };
        AddComponentToRender(_message);

        _warning = new UIText(
            this,
            Font,
            Vector2.Zero,
            new Vector2(WarningScale, WarningScale),
            "Everything built in it goes with it, and it cannot be brought back.")
        {
            Color = _warningColor,
        };
        AddComponentToRender(_warning);

        // Cancel takes the left hand place the other screens give to the button that gets on with things,
        // so that reaching for the usual spot is the harmless one here.
        _cancelButton = new UIButton(this, "Cancel");
        _deleteButton = new UIButton(this, "Delete") { IsDestructive = true };

        Layout();
    }

    /// <summary>Points the screen at the world whose row was pressed.</summary>
    public void Prepare(string worldName)
    {
        WorldName = worldName;
        SetMessage("Delete '" + worldName + "'?");
    }

    public override MenuAction HandleInput(Vector2 mousePosition, bool mousePressed)
    {
        bool cancelPressed = _cancelButton.Update(mousePosition, mousePressed);
        bool deletePressed = _deleteButton.Update(mousePosition, mousePressed);

        // Deliberately not bound to the return key: this is not a screen to get past without looking.
        if (deletePressed)
        {
            return MenuAction.Confirm;
        }

        return cancelPressed ? MenuAction.Back : MenuAction.None;
    }

    protected override void Layout()
    {
        float messageHeight = Font.DesiredPixelLineHeight * MessageScale;
        float warningHeight = Font.DesiredPixelLineHeight * WarningScale;

        float contentHeight =
            messageHeight + LineGap + warningHeight +
            SectionGap + UIButton.Height;

        float contentTop = Math.Max(110, (PixelHeight - contentHeight) / 2.0F);
        float top = contentTop;

        SetMessage(_message.Text);
        _message.PixelPositionInCanvas = new Vector2(CenteredTextLeft(_message.Text, MessageScale), top);
        top += messageHeight + LineGap;

        _warning.PixelPositionInCanvas = new Vector2(CenteredTextLeft(_warning.Text, WarningScale), top);
        top += warningHeight + SectionGap;

        float buttonWidth = (RowWidth - UIButton.Gap) / 2.0F;
        var buttonSize = new Vector2(buttonWidth, UIButton.Height);

        _cancelButton.SetBounds(new Vector2(RowLeft, top), buttonSize);
        _deleteButton.SetBounds(new Vector2(RowLeft + buttonWidth + UIButton.Gap, top), buttonSize);
        top += UIButton.Height;

        LayoutFrame(contentTop, top + 20);
    }

    private void SetMessage(string text)
    {
        _message.Text = text;
        _message.PixelPositionInCanvas = new Vector2(
            CenteredTextLeft(text, MessageScale),
            _message.PixelPositionInCanvas.Y);
    }
}

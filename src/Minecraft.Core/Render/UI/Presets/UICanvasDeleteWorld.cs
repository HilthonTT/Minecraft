using System.Globalization;
using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasDeleteWorld : UICanvasMenu
{
    public const double ArmingSeconds = 1.5;

    private const string DeleteLabel = "Delete Forever";
    private const float LineGap = 12F;

    private readonly UILabel _message;
    private readonly UILabel _warning;
    private readonly UIButton _cancelButton;
    private readonly UIButton _deleteButton;

    private double _shownAt;

    public UICanvasDeleteWorld(Game game)
        : base(game, "Delete World?")
    {
        _message = new UILabel(this, UITheme.HeadingScale, UITheme.Text);
        _warning = new UILabel(
            this,
            UITheme.BodyScale,
            UITheme.Error,
            "Everything built in it goes with it. This cannot be undone.");

        _cancelButton = new UIButton(this, "Keep It", UIButtonStyle.Primary);
        _deleteButton = new UIButton(this, DeleteLabel, UIButtonStyle.Danger);

        SetFocusOrder(_cancelButton, _deleteButton);
        Primary = _cancelButton;

        Layout();
    }

    public string WorldName { get; private set; } = string.Empty;

    protected override string KeyHints => "Enter / Esc  Keep the world      Tab  Move";

    protected override UIWidget? InitialFocus => _cancelButton;

    public static int SecondsUntilArmed(double elapsedSeconds)
    {
        return Math.Max(0, (int)Math.Ceiling(ArmingSeconds - elapsedSeconds));
    }

    public void Prepare(string worldName)
    {
        WorldName = worldName;
        _message.SetFitted("'" + worldName + "' will be deleted.", RowWidth);
        Layout();
    }

    public override void OnShown()
    {
        base.OnShown();
        _shownAt = UIMotion.NowSeconds;
    }

    protected override MenuAction Update(UIInput input)
    {
        int remaining = SecondsUntilArmed(UIMotion.NowSeconds - _shownAt);
        _deleteButton.IsEnabled = remaining == 0;
        _deleteButton.Text = remaining == 0
            ? DeleteLabel
            : DeleteLabel + " (" + remaining.ToString(CultureInfo.InvariantCulture) + ")";

        bool cancelPressed = _cancelButton.Update(input);
        bool deletePressed = _deleteButton.Update(input);

        if (deletePressed)
        {
            return MenuAction.Confirm;
        }

        return cancelPressed ? MenuAction.Back : MenuAction.None;
    }

    protected override void Layout()
    {
        _warning.SetWrapped("Everything built in it goes with it. This cannot be undone.", RowWidth);

        float contentHeight = _message.CapHeight + LineGap + _warning.BlockHeight + UITheme.SectionGap + 8F + UIButton.Height;

        float top = ContentTopFor(contentHeight);
        float contentTop = top;

        _message.PlaceTop(CenteredLeft(_message.Width), top);
        top += _message.CapHeight + LineGap;

        _warning.PlaceTop(CenteredLeft(WidestLine(_warning)), top);
        top += _warning.BlockHeight + UITheme.SectionGap + 8F;

        float buttonWidth = (RowWidth - UITheme.Gap) / 2.0F;
        var buttonSize = new Vector2(buttonWidth, UIButton.Height);

        _cancelButton.SetBounds(new Vector2(RowLeft, top), buttonSize);
        _deleteButton.SetBounds(new Vector2(RowLeft + buttonWidth + UITheme.Gap, top), buttonSize);
        top += UIButton.Height;

        LayoutFrame(contentTop, top);
    }

    private static float WidestLine(UILabel label)
    {
        return label.Text.Split('\n').Max(label.Measure);
    }
}

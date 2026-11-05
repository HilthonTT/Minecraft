using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasMultiplayer : UICanvasMenu
{
    private const int MaxAddressLength = 64;
    private const float LabelToRowGap = 8F;
    private const float DividerGap = 22F;

    private readonly UILabel _joinLabel;
    private readonly UITextField _addressField;
    private readonly UIButton _connectButton;

    private readonly UIImage _divider;

    private readonly UILabel _hostLabel;
    private readonly UIButton _hostButton;
    private readonly UILabel _hostHint;

    private readonly UIButton _backButton;

    public UICanvasMultiplayer(Game game, string defaultAddress, string hostAddress)
        : base(game, "Multiplayer")
    {
        _joinLabel = new UILabel(this, UITheme.SmallScale, UITheme.TextMuted, "JOIN A SERVER");
        _addressField = new UITextField(this, MaxAddressLength) { Placeholder = "host:port", Value = defaultAddress };
        _connectButton = new UIButton(this, "Connect", UIButtonStyle.Primary);

        _divider = UIDraw.Rect(this, UITheme.PanelBorder);

        _hostLabel = new UILabel(this, UITheme.SmallScale, UITheme.TextMuted, "OR HOST ONE YOURSELF");
        _hostButton = new UIButton(this, "Host a World...");
        _hostHint = new UILabel(this, UITheme.SmallScale, UITheme.TextFaint, "Others join you at " + hostAddress, shadow: false);

        _backButton = new UIButton(this, "Back");

        SetFocusOrder(_addressField, _connectButton, _hostButton, _backButton);
        Primary = _connectButton;

        Layout();
    }

    public string Address => _addressField.Value;

    protected override string KeyHints => "Enter  Connect      Tab  Move      Esc  Back";

    protected override UIWidget? InitialFocus => _addressField;

    protected override MenuAction Update(UIInput input)
    {
        _addressField.Update(input);
        _connectButton.IsEnabled = _addressField.Value.Trim().Length > 0;

        bool connectPressed = _connectButton.Update(input);
        bool hostPressed = _hostButton.Update(input);
        bool backPressed = _backButton.Update(input);

        if (connectPressed)
        {
            return MenuAction.Connect;
        }

        if (hostPressed)
        {
            return MenuAction.Host;
        }

        return backPressed ? MenuAction.Back : MenuAction.None;
    }

    protected override void Layout()
    {
        float labelHeight = _joinLabel.CapHeight;
        float hintHeight = _hostHint.CapHeight;

        float contentHeight =
            labelHeight + LabelToRowGap + UIButton.Height +
            (2 * DividerGap) + 1F +
            labelHeight + LabelToRowGap + UIButton.Height + LabelToRowGap + hintHeight +
            UITheme.SectionGap + UIButton.Height;

        float top = ContentTopFor(contentHeight);
        float contentTop = top;
        var rowSize = new Vector2(RowWidth, UIButton.Height);

        _joinLabel.PlaceTop(RowLeft, top);
        top += labelHeight + LabelToRowGap;

        float connectWidth = Math.Min(140F, RowWidth / 3F);
        float fieldWidth = RowWidth - connectWidth - UITheme.Gap;
        _addressField.SetBounds(new Vector2(RowLeft, top), new Vector2(fieldWidth, UIButton.Height));
        _connectButton.SetBounds(new Vector2(RowLeft + fieldWidth + UITheme.Gap, top), new Vector2(connectWidth, UIButton.Height));
        top += UIButton.Height + DividerGap;

        UIDraw.Place(_divider, RowLeft, top, RowWidth, 1F);
        top += 1F + DividerGap;

        _hostLabel.PlaceTop(RowLeft, top);
        top += labelHeight + LabelToRowGap;

        _hostButton.SetBounds(new Vector2(RowLeft, top), rowSize);
        top += UIButton.Height + LabelToRowGap;

        _hostHint.PlaceTop(CenteredLeft(_hostHint.Width), top);
        top += hintHeight + UITheme.SectionGap;

        _backButton.SetBounds(new Vector2(RowLeft, top), rowSize);
        top += UIButton.Height;

        LayoutFrame(contentTop, top);
    }
}

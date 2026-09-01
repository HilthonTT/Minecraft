using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasMultiplayer : UICanvasMenu
{
    private const int MaxAddressLength = 64;
    private const float LabelScale = 0.34F;
    private const float HintScale = 0.28F;

    private const int SectionGap = 26;

    private static readonly Vector3 _backdropColor = new(0.06F, 0.07F, 0.09F);
    private static readonly Vector3 _labelColor = new(0.85F, 0.85F, 0.88F);
    private static readonly Vector3 _hintColor = new(0.62F, 0.62F, 0.66F);

    private readonly UIText _joinLabel;
    private readonly UITextField _addressField;
    private readonly UIButton _connectButton;

    private readonly UIText _hostLabel;
    private readonly UIButton _hostButton;
    private readonly UIText _hostHint;

    private readonly UIButton _backButton;

    public string Address => _addressField.Value;

    public UICanvasMultiplayer(Game game, string defaultAddress, string hostAddress)
        : base(game, "Multiplayer", _backdropColor, 1.0F)
    {
        _joinLabel = AddText("Join a game", LabelScale, _labelColor);
        _addressField = new UITextField(this, MaxAddressLength) { Value = defaultAddress };
        _connectButton = new UIButton(this, "Connect");

        _hostLabel = AddText("Or host one yourself", LabelScale, _labelColor);
        _hostButton = new UIButton(this, "Host Game");
        _hostHint = AddText("Others join you at " + hostAddress, HintScale, _hintColor);

        _backButton = new UIButton(this, "Back");

        Layout();
    }

    public override void OnShown()
    {
        base.OnShown();
        _addressField.HasFocus = true;
    }

    public override MenuAction HandleInput(Vector2 mousePosition, bool mousePressed)
    {
        if (mousePressed)
        {
            _addressField.HasFocus = _addressField.Contains(mousePosition);
        }

        _addressField.Update();

        bool connectPressed = _connectButton.Update(mousePosition, mousePressed);
        bool hostPressed = _hostButton.Update(mousePosition, mousePressed);
        bool backPressed = _backButton.Update(mousePosition, mousePressed);

        if (connectPressed ||
            (_addressField.HasFocus && (Game.Input.OnKeyPress(Keys.Enter) || Game.Input.OnKeyPress(Keys.KeyPadEnter))))
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
        const int labelToRowGap = 6;

        float labelHeight = Font.DesiredPixelLineHeight * LabelScale;
        float hintHeight = Font.DesiredPixelLineHeight * HintScale;

        float contentHeight =
            labelHeight + labelToRowGap + UITextField.Height + UIButton.Gap + UIButton.Height +
            SectionGap +
            labelHeight + labelToRowGap + UIButton.Height + labelToRowGap + hintHeight +
            SectionGap +
            UIButton.Height;

        float contentTop = Math.Max(110, (PixelHeight - contentHeight) / 2.0F);
        float top = contentTop;
        var rowSize = new Vector2(RowWidth, UIButton.Height);

        _joinLabel.PixelPositionInCanvas = new Vector2(RowLeft, top);
        top += labelHeight + labelToRowGap;

        _addressField.SetBounds(new Vector2(RowLeft, top), new Vector2(RowWidth, UITextField.Height));
        top += UITextField.Height + UIButton.Gap;

        _connectButton.SetBounds(new Vector2(RowLeft, top), rowSize);
        top += UIButton.Height + SectionGap;

        _hostLabel.PixelPositionInCanvas = new Vector2(RowLeft, top);
        top += labelHeight + labelToRowGap;

        _hostButton.SetBounds(new Vector2(RowLeft, top), rowSize);
        top += UIButton.Height + labelToRowGap;

        _hostHint.PixelPositionInCanvas = new Vector2(CenteredTextLeft(_hostHint.Text, HintScale), top);
        top += hintHeight + SectionGap;

        _backButton.SetBounds(new Vector2(RowLeft, top), rowSize);
        top += UIButton.Height;

        LayoutFrame(contentTop, top + 20);
    }

    private UIText AddText(string text, float scale, Vector3 color)
    {
        var component = new UIText(this, Font, Vector2.Zero, new Vector2(scale, scale), text) { Color = color };
        AddComponentToRender(component);
        return component;
    }
}

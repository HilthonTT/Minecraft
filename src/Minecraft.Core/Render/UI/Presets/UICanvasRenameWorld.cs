using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasRenameWorld : UICanvasMenu
{
    private const int MaxNameLength = 32;
    private const float LabelToFieldGap = 8F;

    private readonly UILabel _label;
    private readonly UITextField _nameField;
    private readonly UIButton _renameButton;
    private readonly UIButton _cancelButton;

    public UICanvasRenameWorld(Game game)
        : base(game, "Rename World")
    {
        _label = new UILabel(this, UITheme.SmallScale, UITheme.TextMuted, "NEW NAME");
        _nameField = new UITextField(this, MaxNameLength);
        _renameButton = new UIButton(this, "Rename", UIButtonStyle.Primary);
        _cancelButton = new UIButton(this, "Cancel");

        SetFocusOrder(_nameField, _renameButton, _cancelButton);
        Primary = _renameButton;

        Layout();
    }

    public string CurrentName { get; private set; } = string.Empty;

    public string NewName => _nameField.Value;

    protected override string KeyHints => "Enter  Rename      Esc  Cancel";

    protected override UIWidget? InitialFocus => _nameField;

    public void Prepare(string currentName)
    {
        CurrentName = currentName;

        SetSubtitle("Currently '" + currentName + "'");
        _nameField.Value = currentName;
        _nameField.Buffer.SelectAll();
    }

    protected override MenuAction Update(UIInput input)
    {
        _nameField.Update(input);
        _renameButton.IsEnabled = _nameField.Value.Trim().Length > 0;

        bool renamePressed = _renameButton.Update(input);
        bool cancelPressed = _cancelButton.Update(input);

        if (renamePressed)
        {
            return MenuAction.Confirm;
        }

        return cancelPressed ? MenuAction.Back : MenuAction.None;
    }

    protected override void Layout()
    {
        float labelHeight = _label.CapHeight;
        float contentHeight = labelHeight + LabelToFieldGap + UITextField.Height + UITheme.SectionGap + UIButton.Height;

        float top = ContentTopFor(contentHeight);
        float contentTop = top;

        _label.PlaceTop(RowLeft, top);
        top += labelHeight + LabelToFieldGap;

        _nameField.SetBounds(new Vector2(RowLeft, top), new Vector2(RowWidth, UITextField.Height));
        top += UITextField.Height + UITheme.SectionGap;

        float buttonWidth = (RowWidth - UITheme.Gap) / 2.0F;
        var buttonSize = new Vector2(buttonWidth, UIButton.Height);

        _renameButton.SetBounds(new Vector2(RowLeft, top), buttonSize);
        _cancelButton.SetBounds(new Vector2(RowLeft + buttonWidth + UITheme.Gap, top), buttonSize);
        top += UIButton.Height;

        LayoutFrame(contentTop, top);
    }
}

using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>Gives a saved world a new name. Only ever reached from the world list, one world at a time.</summary>
public sealed class UICanvasRenameWorld : UICanvasMenu
{
    private const int MaxNameLength = 32;
    private const float LabelScale = 0.32F;
    private const int LabelToFieldGap = 6;
    private const int SectionGap = 20;

    private static readonly Vector3 _backdropColor = new(0.06F, 0.07F, 0.09F);
    private static readonly Vector3 _labelColor = new(0.85F, 0.85F, 0.88F);

    private readonly UIText _label;
    private readonly UITextField _nameField;
    private readonly UIButton _renameButton;
    private readonly UIButton _cancelButton;

    /// <summary>The world this screen was opened for, kept so the rename knows what it is renaming.</summary>
    public string CurrentName { get; private set; } = string.Empty;

    /// <summary>What was typed into the box.</summary>
    public string NewName => _nameField.Value;

    public UICanvasRenameWorld(Game game)
        : base(game, "Rename World", _backdropColor, 1.0F)
    {
        _label = new UIText(this, Font, Vector2.Zero, new Vector2(LabelScale, LabelScale), string.Empty)
        {
            Color = _labelColor,
        };
        AddComponentToRender(_label);

        _nameField = new UITextField(this, MaxNameLength);
        _renameButton = new UIButton(this, "Rename");
        _cancelButton = new UIButton(this, "Cancel");

        Layout();
    }

    /// <summary>Points the screen at the world whose row was pressed.</summary>
    public void Prepare(string currentName)
    {
        CurrentName = currentName;

        _label.Text = "New name for '" + currentName + "'";
        _nameField.Value = currentName;
    }

    public override void OnShown()
    {
        base.OnShown();
        _nameField.HasFocus = true;
    }

    public override MenuAction HandleInput(Vector2 mousePosition, bool mousePressed)
    {
        _nameField.Update();

        bool renamePressed = _renameButton.Update(mousePosition, mousePressed);
        bool cancelPressed = _cancelButton.Update(mousePosition, mousePressed);

        if (renamePressed || Game.Input.OnKeyPress(Keys.Enter) || Game.Input.OnKeyPress(Keys.KeyPadEnter))
        {
            return MenuAction.Confirm;
        }

        return cancelPressed ? MenuAction.Back : MenuAction.None;
    }

    protected override void Layout()
    {
        float labelHeight = Font.DesiredPixelLineHeight * LabelScale;

        float contentHeight =
            labelHeight + LabelToFieldGap + UITextField.Height +
            SectionGap + UIButton.Height;

        float contentTop = Math.Max(110, (PixelHeight - contentHeight) / 2.0F);
        float top = contentTop;

        _label.PixelPositionInCanvas = new Vector2(RowLeft, top);
        top += labelHeight + LabelToFieldGap;

        _nameField.SetBounds(new Vector2(RowLeft, top), new Vector2(RowWidth, UITextField.Height));
        top += UITextField.Height + SectionGap;

        float buttonWidth = (RowWidth - UIButton.Gap) / 2.0F;
        var buttonSize = new Vector2(buttonWidth, UIButton.Height);

        _renameButton.SetBounds(new Vector2(RowLeft, top), buttonSize);
        _cancelButton.SetBounds(new Vector2(RowLeft + buttonWidth + UIButton.Gap, top), buttonSize);
        top += UIButton.Height;

        LayoutFrame(contentTop, top + 20);
    }
}

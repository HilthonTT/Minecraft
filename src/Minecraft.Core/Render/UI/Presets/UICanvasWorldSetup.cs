using Minecraft.Core.Games;
using Minecraft.Core.Worlds.Generation;
using Minecraft.Core.Worlds.Storage;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Globalization;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// Names the world to play and picks the seed it is generated from. A seed only ever decides a world that
/// does not exist yet, so the screen keeps saying which of the two the current name would do, and the button
/// that starts the game says the same thing.
/// </summary>
public sealed class UICanvasWorldSetup : UICanvasMenu
{
    private const int MaxNameLength = 32;
    private const int MaxSeedLength = 24;

    private const float LabelScale = 0.34F;
    private const float PreviewScale = 0.28F;
    private const int RandomButtonWidth = 120;
    private const int LabelToFieldGap = 6;
    private const int SectionGap = 16;

    private static readonly Vector3 _backdropColor = new(0.06F, 0.07F, 0.09F);
    private static readonly Vector3 _labelColor = new(0.85F, 0.85F, 0.88F);
    private static readonly Vector3 _previewColor = new(0.62F, 0.62F, 0.66F);

    private readonly string _savesRoot;

    private readonly UIText _nameLabel;
    private readonly UITextField _nameField;
    private readonly UIText _seedLabel;
    private readonly UITextField _seedField;
    private readonly UIButton _randomSeedButton;
    private readonly UIText _preview;
    private readonly UIButton _playButton;
    private readonly UIButton _backButton;

    /// <summary>What the preview was last worked out for, so the save directory is not read every frame.</summary>
    private string _previewedName = string.Empty;
    private string _previewedSeed = string.Empty;

    public string WorldName => _nameField.Value;

    public string SeedText => _seedField.Value;

    public UICanvasWorldSetup(Game game, string savesRoot)
        : base(game, "Singleplayer", _backdropColor, 1.0F)
    {
        _savesRoot = savesRoot;

        _nameLabel = AddText("World name", LabelScale, _labelColor);
        _nameField = new UITextField(this, MaxNameLength);

        _seedLabel = AddText("Seed", LabelScale, _labelColor);
        _seedField = new UITextField(this, MaxSeedLength);
        _randomSeedButton = new UIButton(this, "Random");

        _preview = AddText(string.Empty, PreviewScale, _previewColor);

        _playButton = new UIButton(this, "Create World");
        _backButton = new UIButton(this, "Back");

        Layout();
    }

    /// <summary>
    /// Points the screen at a world name before it is shown. The suggested name is one nothing is saved
    /// under, so that arriving here and pressing play generates a world rather than reopening the last one.
    /// </summary>
    public void Prepare(string suggestedName)
    {
        _nameField.Value = suggestedName;
        _seedField.Value = string.Empty;
        RefreshPreview();
    }

    public override void OnShown()
    {
        base.OnShown();

        Focus(_nameField);
        RefreshPreview();
    }

    public override MenuAction HandleInput(Vector2 mousePosition, bool mousePressed)
    {
        UpdateFocus(mousePosition, mousePressed);

        _nameField.Update();
        _seedField.Update();

        if (_randomSeedButton.Update(mousePosition, mousePressed))
        {
            // Written into the box rather than left blank, so that whatever comes up can be read off and
            // used again.
            _seedField.Value = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
            Focus(_seedField);
        }

        RefreshPreview();

        bool playPressed = _playButton.Update(mousePosition, mousePressed);
        bool backPressed = _backButton.Update(mousePosition, mousePressed);

        if (playPressed || Game.Input.OnKeyPress(Keys.Enter) || Game.Input.OnKeyPress(Keys.KeyPadEnter))
        {
            return MenuAction.Play;
        }

        return backPressed ? MenuAction.Back : MenuAction.None;
    }

    /// <summary>Clicking decides which box is typed into, and tab steps between them.</summary>
    private void UpdateFocus(Vector2 mousePosition, bool mousePressed)
    {
        if (mousePressed)
        {
            // A click on neither box leaves the focus where it was, so that pressing a button does not also
            // take the keyboard away from what was being typed.
            if (_nameField.Contains(mousePosition))
            {
                Focus(_nameField);
            }
            else if (_seedField.Contains(mousePosition))
            {
                Focus(_seedField);
            }
        }

        if (Game.Input.OnKeyPress(Keys.Tab))
        {
            Focus(_nameField.HasFocus ? _seedField : _nameField);
        }
    }

    /// <summary>Gives one box the keyboard, which means taking it away from the other.</summary>
    private void Focus(UITextField field)
    {
        _nameField.HasFocus = field == _nameField;
        _seedField.HasFocus = field == _seedField;
    }

    /// <summary>
    /// Says what pressing play would do with what has been typed. Only worked out again when something
    /// changed, since answering it means looking at the saves directory.
    /// </summary>
    private void RefreshPreview()
    {
        if (_previewedName == _nameField.Value && _previewedSeed == _seedField.Value)
        {
            return;
        }

        _previewedName = _nameField.Value;
        _previewedSeed = _seedField.Value;

        string name = _nameField.Value.Trim();
        if (name.Length == 0)
        {
            SetPreview("Give the world a name.");
            _playButton.IsEnabled = false;
            return;
        }

        _playButton.IsEnabled = true;

        // What the world will really be called, which is not always what was typed.
        string savedAs = WorldStorage.SanitiseWorldName(name);

        if (WorldStorage.WorldExists(_savesRoot, name))
        {
            _playButton.Text = "Continue World";
            SetPreview("'" + savedAs + "' already exists, so it is carried on and keeps its own seed.");
            return;
        }

        _playButton.Text = "Create World";

        int? seed = SeedParser.Parse(_seedField.Value);
        SetPreview(seed is null
            ? "A new world called '" + savedAs + "', from a seed picked at random."
            : "A new world called '" + savedAs + "', from seed " + seed.Value + ".");
    }

    private void SetPreview(string text)
    {
        _preview.Text = text;
        _preview.PixelPositionInCanvas = new Vector2(
            CenteredTextLeft(text, PreviewScale),
            _preview.PixelPositionInCanvas.Y);
    }

    protected override void Layout()
    {
        float labelHeight = Font.DesiredPixelLineHeight * LabelScale;
        float previewHeight = Font.DesiredPixelLineHeight * PreviewScale;

        float contentHeight =
            labelHeight + LabelToFieldGap + UITextField.Height +
            SectionGap +
            labelHeight + LabelToFieldGap + UITextField.Height +
            SectionGap + previewHeight +
            SectionGap + UIButton.Height;

        float contentTop = Math.Max(110, (PixelHeight - contentHeight) / 2.0F);
        float top = contentTop;

        _nameLabel.PixelPositionInCanvas = new Vector2(RowLeft, top);
        top += labelHeight + LabelToFieldGap;

        _nameField.SetBounds(new Vector2(RowLeft, top), new Vector2(RowWidth, UITextField.Height));
        top += UITextField.Height + SectionGap;

        _seedLabel.PixelPositionInCanvas = new Vector2(RowLeft, top);
        top += labelHeight + LabelToFieldGap;

        // The seed box gives up its right hand end to the button that fills it in.
        float randomWidth = Math.Min(RandomButtonWidth, RowWidth / 3.0F);
        float seedWidth = RowWidth - randomWidth - UIButton.Gap;

        _seedField.SetBounds(new Vector2(RowLeft, top), new Vector2(seedWidth, UITextField.Height));
        _randomSeedButton.SetBounds(
            new Vector2(RowLeft + seedWidth + UIButton.Gap, top),
            new Vector2(randomWidth, UITextField.Height));
        top += UITextField.Height + SectionGap;

        _preview.PixelPositionInCanvas = new Vector2(CenteredTextLeft(_preview.Text, PreviewScale), top);
        top += previewHeight + SectionGap;

        // Not an even split: the button that starts the game carries the longer of the two labels, since it
        // says which of creating and continuing pressing it would do.
        float playWidth = (RowWidth - UIButton.Gap) * 0.65F;
        float backWidth = RowWidth - UIButton.Gap - playWidth;

        _playButton.SetBounds(new Vector2(RowLeft, top), new Vector2(playWidth, UIButton.Height));
        _backButton.SetBounds(
            new Vector2(RowLeft + playWidth + UIButton.Gap, top),
            new Vector2(backWidth, UIButton.Height));
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

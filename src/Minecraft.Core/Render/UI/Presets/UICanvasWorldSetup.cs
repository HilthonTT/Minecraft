using System.Globalization;
using Minecraft.Core.Games;
using Minecraft.Core.Worlds.Generation;
using Minecraft.Core.Worlds.Storage;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasWorldSetup : UICanvasMenu
{
    private const int MaxNameLength = 32;
    private const int MaxSeedLength = 24;

    private const string RandomLabel = "Randomise";
    private const float LabelToFieldGap = 8F;
    private const float PreviewLines = 2F;

    private static readonly string[] _gameModeNames = ["Survival", "Creative"];

    private readonly string _savesRoot;

    private readonly UILabel _nameLabel;
    private readonly UITextField _nameField;
    private readonly UILabel _seedLabel;
    private readonly UITextField _seedField;
    private readonly UIButton _randomSeedButton;
    private readonly UILabel _gameModeLabel;
    private readonly UICycleButton _gameModeButton;
    private readonly UILabel _gameModeHint;
    private readonly UILabel _preview;
    private readonly UIButton _playButton;
    private readonly UIButton _backButton;

    private string _previewedName = string.Empty;
    private string _previewedSeed = string.Empty;
    private GameMode _previewedGameMode = GameMode.Creative;
    private bool _previewDirty = true;

    public UICanvasWorldSetup(Game game, string savesRoot)
        : base(game, "Create World")
    {
        _savesRoot = savesRoot;

        _nameLabel = new UILabel(this, UITheme.SmallScale, UITheme.TextMuted, "WORLD NAME");
        _nameField = new UITextField(this, MaxNameLength) { Placeholder = "My World" };

        _seedLabel = new UILabel(this, UITheme.SmallScale, UITheme.TextMuted, "SEED");
        _seedField = new UITextField(this, MaxSeedLength) { Placeholder = "Leave blank for a random seed" };
        _randomSeedButton = new UIButton(this, RandomLabel);

        _gameModeLabel = new UILabel(this, UITheme.SmallScale, UITheme.TextMuted, "GAME MODE");
        _gameModeButton = new UICycleButton(this, _gameModeNames, IndexOf(ArgsParser.DefaultGameMode));
        _gameModeHint = new UILabel(this, UITheme.SmallScale, UITheme.TextMuted, string.Empty, shadow: false);

        _preview = new UILabel(this, UITheme.SmallScale, UITheme.TextFaint, string.Empty, shadow: false);

        _playButton = new UIButton(this, "Create World", UIButtonStyle.Primary);
        _backButton = new UIButton(this, "Cancel");

        SetFocusOrder(_nameField, _seedField, _randomSeedButton, _gameModeButton, _playButton, _backButton);
        Primary = _playButton;

        Layout();
    }

    public string WorldName => _nameField.Value;

    public string SeedText => _seedField.Value;

    public GameMode GameMode => _gameModeButton.SelectedIndex == 0 ? GameMode.Survival : GameMode.Creative;

    protected override float MaxRowWidth => 480;

    protected override string KeyHints => "Enter  Create      Tab  Next field      Esc  Cancel";

    protected override UIWidget? InitialFocus => _nameField;

    public void Prepare(string suggestedName)
    {
        _nameField.Value = suggestedName;
        _nameField.Buffer.SelectAll();
        _seedField.Value = string.Empty;
        _gameModeButton.SelectedIndex = IndexOf(ArgsParser.DefaultGameMode);

        _previewDirty = true;
        RefreshPreview();
    }

    public override void OnShown()
    {
        base.OnShown();

        _previewDirty = true;
        RefreshPreview();
    }

    protected override MenuAction Update(UIInput input)
    {
        _nameField.Update(input);
        _seedField.Update(input);

        if (_randomSeedButton.Update(input))
        {
            _seedField.Value = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
        }

        _gameModeButton.Update(input);

        RefreshPreview();

        bool playPressed = _playButton.Update(input);
        bool backPressed = _backButton.Update(input);

        if (playPressed)
        {
            return MenuAction.Play;
        }

        return backPressed ? MenuAction.Back : MenuAction.None;
    }

    private static int IndexOf(GameMode gameMode) => gameMode == GameMode.Survival ? 0 : 1;

    private void RefreshPreview()
    {
        if (!_previewDirty &&
            _previewedName == _nameField.Value &&
            _previewedSeed == _seedField.Value &&
            _previewedGameMode == GameMode)
        {
            return;
        }

        _previewDirty = false;
        _previewedName = _nameField.Value;
        _previewedSeed = _seedField.Value;
        _previewedGameMode = GameMode;

        _gameModeHint.SetWrapped(
            GameMode == GameMode.Survival
                ? "Gather resources, craft tools, and keep yourself fed and alive."
                : "Every block on hand, nothing to lose. Build whatever you like.",
            RowWidth);

        string name = _nameField.Value.Trim();
        if (name.Length == 0)
        {
            SetPreview("Give the world a name to create it.", UITheme.Warning);
            _playButton.IsEnabled = false;
            return;
        }

        _playButton.IsEnabled = true;

        string savedAs = WorldSaves.SanitiseWorldName(name);

        if (WorldSaves.WorldExists(_savesRoot, name))
        {
            _playButton.Text = "Continue World";
            SetPreview("'" + savedAs + "' already exists, so it opens with its own seed and mode.", UITheme.Warning);
            return;
        }

        _playButton.Text = "Create World";

        int? seed = SeedParser.Parse(_seedField.Value);
        string from = seed is null ? "a random seed" : "seed " + seed.Value.ToString(CultureInfo.InvariantCulture);
        SetPreview("Saved as '" + savedAs + "', generated from " + from + ".", UITheme.TextFaint);
    }

    private void SetPreview(string text, Vector3 color)
    {
        _preview.Color = color;
        _preview.SetWrapped(text, RowWidth);
    }

    protected override void Layout()
    {
        float labelHeight = _nameLabel.CapHeight;
        float hintHeight = _gameModeHint.LineStep * 2F;
        float previewHeight = _preview.LineStep * PreviewLines;
        float sectionGap = UITheme.SectionGap;

        float contentHeight =
            labelHeight + LabelToFieldGap + UITextField.Height + sectionGap +
            labelHeight + LabelToFieldGap + UITextField.Height + sectionGap +
            labelHeight + LabelToFieldGap + UIButton.Height + UITheme.Gap + hintHeight +
            previewHeight + UITheme.Gap + UIButton.Height;

        float top = ContentTopFor(contentHeight);
        float contentTop = top;

        _nameLabel.PlaceTop(RowLeft, top);
        top += labelHeight + LabelToFieldGap;
        _nameField.SetBounds(new Vector2(RowLeft, top), new Vector2(RowWidth, UITextField.Height));
        top += UITextField.Height + sectionGap;

        _seedLabel.PlaceTop(RowLeft, top);
        top += labelHeight + LabelToFieldGap;

        float randomWidth = Math.Min(UIButton.MeasureRequiredWidth(RandomLabel) + 8F, RowWidth / 2.5F);
        float seedWidth = RowWidth - randomWidth - UITheme.Gap;
        _seedField.SetBounds(new Vector2(RowLeft, top), new Vector2(seedWidth, UITextField.Height));
        _randomSeedButton.SetBounds(new Vector2(RowLeft + seedWidth + UITheme.Gap, top), new Vector2(randomWidth, UITextField.Height));
        top += UITextField.Height + sectionGap;

        _gameModeLabel.PlaceTop(RowLeft, top);
        top += labelHeight + LabelToFieldGap;
        _gameModeButton.SetBounds(new Vector2(RowLeft, top), new Vector2(RowWidth, UIButton.Height));
        top += UIButton.Height + UITheme.Gap;

        _gameModeHint.PlaceTop(RowLeft, top);
        top += hintHeight;

        _preview.PlaceTop(RowLeft, top);
        top += previewHeight + UITheme.Gap;

        float playWidth = (RowWidth - UITheme.Gap) * 0.62F;
        _playButton.SetBounds(new Vector2(RowLeft, top), new Vector2(playWidth, UIButton.Height));
        _backButton.SetBounds(
            new Vector2(RowLeft + playWidth + UITheme.Gap, top),
            new Vector2(RowWidth - playWidth - UITheme.Gap, UIButton.Height));
        top += UIButton.Height;

        LayoutFrame(contentTop, top);

        _previewDirty = true;
    }
}

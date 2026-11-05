using System.Globalization;
using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasOptions : UICanvasMenu
{
    private const float SectionHeight = 30F;
    private const float ControlMaxWidth = 270F;
    private const float ScrollStep = 48F;
    private const float ScrollbarWidth = 4F;
    private const float ReservedChrome = 120F;

    private static readonly int[] _guiScalePercents = [75, 85, 100, 115, 130, 150];

    private readonly GameSettings _settings;
    private readonly List<OptionRow> _rows = [];
    private readonly UIButton _doneButton;
    private readonly UIImage _scrollTrack;
    private readonly UIImage _scrollThumb;

    private float _scroll;
    private float _maxScroll;
    private float _viewportTop;
    private float _viewportHeight;

    public UICanvasOptions(Game game)
        : base(game, "Options")
    {
        _settings = game.Settings;

        Section("Video");
        Slider(
            "Render Distance",
            GameSettings.MinRenderDistanceChunks,
            GameSettings.MaxRenderDistanceChunks,
            1F,
            () => _settings.RenderDistanceChunks,
            value => _settings.RenderDistanceChunks = (int)MathF.Round(value),
            value => Whole(value) + (MathF.Round(value) == 1 ? " chunk" : " chunks"));
        Slider(
            "Field of View",
            GameSettings.MinFieldOfViewDegrees,
            GameSettings.MaxFieldOfViewDegrees,
            1F,
            () => _settings.FieldOfViewDegrees,
            value => _settings.FieldOfViewDegrees = MathF.Round(value),
            value => Whole(value) + " degrees");
        Toggle(
            "Shaders",
            () => _settings.RealisticShaders,
            value => _settings.RealisticShaders = value,
            "Realistic",
            "Off");

        Section("Interface");
        Cycle(
            "GUI Scale",
            [.. _guiScalePercents.Select(percent => percent.ToString(CultureInfo.InvariantCulture) + "%")],
            () => NearestGuiScaleIndex(_settings.GuiScalePercent),
            index => _settings.GuiScalePercent = _guiScalePercents[index]);
        Toggle("Show FPS", () => _settings.ShowFps, value => _settings.ShowFps = value);

        Section("Audio");
        Slider("Master Volume", 0F, 1F, 0.01F, () => _settings.MasterVolume, value => _settings.MasterVolume = value, Percentage);

        Section("Controls");
        Slider(
            "Mouse Sensitivity",
            GameSettings.MinMouseSensitivity,
            GameSettings.MaxMouseSensitivity,
            0.05F,
            () => _settings.MouseSensitivity,
            value => _settings.MouseSensitivity = value,
            Percentage);

        _doneButton = new UIButton(this, "Done", UIButtonStyle.Primary);
        _scrollTrack = UIDraw.Rect(this, UITheme.Field, 0.8F);
        _scrollThumb = UIDraw.Rect(this, UITheme.TextFaint);

        SetFocusOrder([.. _rows.Where(row => row.Control is not null).Select(row => row.Control!), _doneButton]);
        Primary = _doneButton;

        Layout();
    }

    protected override float MaxRowWidth => 540;

    protected override string KeyHints => "Esc  Done      Tab / Arrows  Move      Left / Right  Change";

    public static int NearestGuiScaleIndex(int percent)
    {
        int best = 0;
        for (int index = 1; index < _guiScalePercents.Length; index++)
        {
            if (Math.Abs(_guiScalePercents[index] - percent) < Math.Abs(_guiScalePercents[best] - percent))
            {
                best = index;
            }
        }

        return best;
    }

    public override void OnShown()
    {
        foreach (OptionRow row in _rows)
        {
            row.Sync();
        }

        _scroll = 0F;
        Layout();

        base.OnShown();
    }

    protected override MenuAction Update(UIInput input)
    {
        UpdateScrolling(input);

        foreach (OptionRow row in _rows)
        {
            row.Update(input);
        }

        return _doneButton.Update(input) ? MenuAction.Back : MenuAction.None;
    }

    protected override void OnFocusChanged(UIWidget? widget)
    {
        OptionRow? row = _rows.FirstOrDefault(candidate => candidate.Control == widget);
        if (row is null || _viewportHeight <= 0F)
        {
            return;
        }

        float rowTop = row.Offset - _scroll;
        float rowBottom = rowTop + row.Height;

        if (rowTop < 0F)
        {
            ScrollTo(row.Offset - (row.Index > 0 && _rows[row.Index - 1].IsSection ? SectionHeight : 0F));
        }
        else if (rowBottom > _viewportHeight)
        {
            ScrollTo(row.Offset + row.Height - _viewportHeight);
        }
    }

    protected override void Layout()
    {
        float offset = 0F;
        foreach (OptionRow row in _rows)
        {
            row.Offset = offset;
            offset += row.Height + (row.IsSection ? 0F : UITheme.Gap);
        }

        float rowsHeight = Math.Max(0F, offset - UITheme.Gap);
        float doneBlock = UITheme.SectionGap + UIButton.Height;
        float available = PixelHeight - TitleBlockHeight - (2 * UITheme.Padding) - doneBlock - ReservedChrome;

        _viewportHeight = Math.Clamp(available, Math.Min(rowsHeight, UITheme.RowHeight + SectionHeight), rowsHeight);
        _maxScroll = Math.Max(0F, rowsHeight - _viewportHeight);
        _scroll = Math.Clamp(_scroll, 0F, _maxScroll);

        _viewportTop = ContentTopFor(_viewportHeight + doneBlock);

        float controlWidth = Math.Min(ControlMaxWidth, RowWidth * 0.55F);
        float controlLeft = RowLeft + RowWidth - controlWidth;

        foreach (OptionRow row in _rows)
        {
            float top = _viewportTop + row.Offset - _scroll;
            bool visible = top >= _viewportTop - 0.5F && top + row.Height <= _viewportTop + _viewportHeight + 0.5F;

            row.Label.IsVisible = visible;

            if (row.IsSection)
            {
                row.Rule!.IsVisible = visible;
                row.Label.PlaceTop(RowLeft, top + SectionHeight - 10F - row.Label.CapHeight);
                UIDraw.Place(row.Rule, RowLeft + row.Label.Width + 10F, top + SectionHeight - 10F - (row.Label.CapHeight / 2F), RowWidth - row.Label.Width - 10F, 1F);
                continue;
            }

            row.Control!.IsVisible = visible;
            row.Label.SetFitted(row.Name, RowWidth - controlWidth - UITheme.SectionGap);
            row.Label.PlaceMiddle(RowLeft, top + (row.Height / 2F));
            row.Control.SetBounds(new Vector2(controlLeft, top), new Vector2(controlWidth, row.Height));
        }

        LayoutScrollbar();

        float doneTop = _viewportTop + _viewportHeight + UITheme.SectionGap;
        _doneButton.SetBounds(new Vector2(RowLeft + ((RowWidth - 240F) / 2F), doneTop), new Vector2(Math.Min(240F, RowWidth), UIButton.Height));

        LayoutFrame(_viewportTop, doneTop + UIButton.Height);
    }

    private void Section(string title)
    {
        var label = new UILabel(this, UITheme.SmallScale, UITheme.Accent, title.ToUpperInvariant());
        UIImage rule = UIDraw.Rect(this, UITheme.PanelBorder);

        _rows.Add(new OptionRow(_rows.Count, title, label, null, SectionHeight) { Rule = rule });
    }

    private void Slider(
        string name,
        float minimum,
        float maximum,
        float step,
        Func<float> read,
        Action<float> write,
        Func<float, string> describe)
    {
        var slider = new UISlider(this, minimum, maximum, step, describe);
        AddRow(name, slider, () => slider.Value = read(), input =>
        {
            if (slider.Update(input))
            {
                write(slider.Value);
            }
        });
    }

    private void Toggle(string name, Func<bool> read, Action<bool> write, string onText = "On", string offText = "Off")
    {
        var toggle = new UIToggle(this, read(), onText, offText);
        AddRow(name, toggle, () => toggle.Value = read(), input =>
        {
            if (toggle.Update(input))
            {
                write(toggle.Value);
            }
        });
    }

    private void Cycle(string name, IReadOnlyList<string> options, Func<int> read, Action<int> write)
    {
        var cycle = new UICycleButton(this, options, read());
        AddRow(name, cycle, () => cycle.SelectedIndex = read(), input =>
        {
            if (cycle.Update(input))
            {
                write(cycle.SelectedIndex);
            }
        });
    }

    private void AddRow(string name, UIWidget control, Action sync, Action<UIInput> update)
    {
        var label = new UILabel(this, UITheme.LabelScale, UITheme.Text, name);
        _rows.Add(new OptionRow(_rows.Count, name, label, control, UITheme.RowHeight) { Sync = sync, Update = update });
    }

    private void UpdateScrolling(UIInput input)
    {
        if (_maxScroll <= 0F || input.Scroll == 0F)
        {
            return;
        }

        ScrollTo(_scroll - (MathF.Sign(input.Scroll) * ScrollStep));
    }

    private void ScrollTo(float scroll)
    {
        float clamped = Math.Clamp(scroll, 0F, _maxScroll);
        if (clamped == _scroll)
        {
            return;
        }

        _scroll = clamped;
        Layout();
    }

    private void LayoutScrollbar()
    {
        bool scrollable = _maxScroll > 0F;
        _scrollTrack.IsVisible = scrollable;
        _scrollThumb.IsVisible = scrollable;

        if (!scrollable)
        {
            return;
        }

        float left = RowLeft + RowWidth + (UITheme.Padding / 2F) - (ScrollbarWidth / 2F);
        float total = _viewportHeight + _maxScroll;
        float thumbHeight = Math.Max(24F, _viewportHeight * (_viewportHeight / total));
        float thumbTop = _viewportTop + ((_viewportHeight - thumbHeight) * (_scroll / _maxScroll));

        UIDraw.Place(_scrollTrack, left, _viewportTop, ScrollbarWidth, _viewportHeight);
        UIDraw.Place(_scrollThumb, left, thumbTop, ScrollbarWidth, thumbHeight);
    }

    private static string Whole(float value)
    {
        return MathF.Round(value).ToString("0", CultureInfo.InvariantCulture);
    }

    private static string Percentage(float fraction)
    {
        return MathF.Round(fraction * 100F).ToString("0", CultureInfo.InvariantCulture) + "%";
    }

    private sealed class OptionRow(int index, string name, UILabel label, UIWidget? control, float height)
    {
        public int Index { get; } = index;

        public string Name { get; } = name;

        public UILabel Label { get; } = label;

        public UIWidget? Control { get; } = control;

        public float Height { get; } = height;

        public bool IsSection => Control is null;

        public UIImage? Rule { get; init; }

        public float Offset { get; set; }

        public Action Sync { get; init; } = () => { };

        public Action<UIInput> Update { get; init; } = _ => { };
    }
}

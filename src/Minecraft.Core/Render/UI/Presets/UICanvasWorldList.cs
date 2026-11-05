using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasWorldList : UICanvasMenu
{
    private const int MaxVisibleRows = 8;
    private const int MinVisibleRows = 2;
    private const float ReservedChrome = 110F;

    private readonly string _savesRoot;
    private readonly List<WorldSummary> _worlds = [];

    private readonly UIList _list;
    private readonly UIButton _playButton;
    private readonly UIButton _createButton;
    private readonly UIButton _renameButton;
    private readonly UIButton _deleteButton;
    private readonly UIButton _backButton;

    public UICanvasWorldList(Game game, string savesRoot)
        : base(game, "Singleplayer")
    {
        _savesRoot = savesRoot;

        _list = new UIList(this, MaxVisibleRows)
        {
            EmptyTitle = "No worlds yet",
            EmptyDetail = "Create a new world to start playing.",
        };

        _playButton = new UIButton(this, "Play Selected World", UIButtonStyle.Primary);
        _createButton = new UIButton(this, "Create New World");
        _renameButton = new UIButton(this, "Rename");
        _deleteButton = new UIButton(this, "Delete", UIButtonStyle.Danger);
        _backButton = new UIButton(this, "Back");

        SetFocusOrder(_list, _playButton, _createButton, _renameButton, _deleteButton, _backButton);
        Primary = _playButton;

        Layout();
    }

    public string SelectedWorld => _list.SelectedIndex >= 0 && _list.SelectedIndex < _worlds.Count
        ? _worlds[_list.SelectedIndex].Name
        : string.Empty;

    protected override float MaxRowWidth => 620;

    protected override string KeyHints => "Enter  Play      F2  Rename      Del  Delete      Esc  Back";

    protected override UIWidget? InitialFocus => _worlds.Count > 0 ? _list : _createButton;

    public void SetWorlds(IReadOnlyList<string> worlds)
    {
        string previous = SelectedWorld;

        _worlds.Clear();
        _worlds.AddRange(worlds.Select(name => WorldSummary.Read(_savesRoot, name)));

        DateTime now = DateTime.Now;
        _list.SetItems([.. _worlds.Select(world => new UIListItem(world.Name, world.Describe(now)))], 0);
        Select(previous);

        RefreshButtons();
    }

    public void Select(string worldName)
    {
        int index = _worlds.FindIndex(world => string.Equals(world.Name, worldName, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            _list.SelectedIndex = index;
        }
    }

    protected override MenuAction Update(UIInput input)
    {
        _list.Update(input);

        bool play = _playButton.Update(input) || _list.WasActivated;
        bool create = _createButton.Update(input);
        bool rename = _renameButton.Update(input);
        bool delete = _deleteButton.Update(input);
        bool back = _backButton.Update(input);

        bool hasSelection = SelectedWorld.Length > 0;
        bool typing = FocusedWidget is { WantsText: true };

        if (hasSelection && !typing)
        {
            rename |= UIInput.KeyPressed(Keys.F2);
            delete |= UIInput.KeyPressed(Keys.Delete);
        }

        if (play && hasSelection)
        {
            return MenuAction.PlaySelected;
        }

        if (create)
        {
            return MenuAction.CreateWorld;
        }

        if (rename && hasSelection)
        {
            return MenuAction.RenameSelected;
        }

        if (delete && hasSelection)
        {
            return MenuAction.DeleteSelected;
        }

        return back ? MenuAction.Back : MenuAction.None;
    }

    protected override void Layout()
    {
        float gap = UITheme.RowGap;
        float buttonsBlock = UITheme.SectionGap + (2 * UIButton.Height) + gap;

        float available = PixelHeight - TitleBlockHeight - (2 * UITheme.Padding) - buttonsBlock - ReservedChrome;
        int rows = MinVisibleRows;
        while (rows < MaxVisibleRows && UIList.HeightFor(rows + 1) <= available)
        {
            rows++;
        }

        float listHeight = UIList.HeightFor(rows);
        float top = ContentTopFor(listHeight + buttonsBlock);
        float contentTop = top;

        _list.SetBounds(new Vector2(RowLeft, top), new Vector2(RowWidth, listHeight));
        top += listHeight + UITheme.SectionGap;

        float half = (RowWidth - gap) / 2F;
        _playButton.SetBounds(new Vector2(RowLeft, top), new Vector2(half, UIButton.Height));
        _createButton.SetBounds(new Vector2(RowLeft + half + gap, top), new Vector2(half, UIButton.Height));
        top += UIButton.Height + gap;

        float third = (RowWidth - (2 * gap)) / 3F;
        _renameButton.SetBounds(new Vector2(RowLeft, top), new Vector2(third, UIButton.Height));
        _deleteButton.SetBounds(new Vector2(RowLeft + third + gap, top), new Vector2(third, UIButton.Height));
        _backButton.SetBounds(new Vector2(RowLeft + (2 * (third + gap)), top), new Vector2(third, UIButton.Height));
        top += UIButton.Height;

        LayoutFrame(contentTop, top);
    }

    private void RefreshButtons()
    {
        bool hasWorlds = _worlds.Count > 0;

        _playButton.IsEnabled = hasWorlds;
        _renameButton.IsEnabled = hasWorlds;
        _deleteButton.IsEnabled = hasWorlds;

        _playButton.Style = hasWorlds ? UIButtonStyle.Primary : UIButtonStyle.Normal;
        _createButton.Style = hasWorlds ? UIButtonStyle.Normal : UIButtonStyle.Primary;
        Primary = hasWorlds ? _playButton : _createButton;
    }
}

using Minecraft.Core.Games;
using OpenTK.Mathematics;
using System.Globalization;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// The worlds already saved, most recently played first. A row plays its world, and carries the two things
/// that can be done to it without opening it. More worlds than fit are reached by scrolling, since the rows
/// are a fixed set that the list is shown through rather than one widget per world.
/// </summary>
public sealed class UICanvasWorldList : UICanvasMenu
{
    /// <summary>How many rows exist. The list scrolls through them when it is longer than this.</summary>
    private const int MaxVisibleRows = 6;

    private const int RowGap = 8;

    private const string RenameLabel = "Rename";
    private const string DeleteLabel = "Delete";
    private const int LinesPerScrollStep = 1;
    private const float MessageScale = 0.30F;

    private static readonly Vector3 _backdropColor = new(0.06F, 0.07F, 0.09F);
    private static readonly Vector3 _messageColor = new(0.62F, 0.62F, 0.66F);

    private readonly List<string> _worlds = [];

    private readonly UIButton[] _playButtons = new UIButton[MaxVisibleRows];
    private readonly UIButton[] _renameButtons = new UIButton[MaxVisibleRows];
    private readonly UIButton[] _deleteButtons = new UIButton[MaxVisibleRows];

    private readonly UIText _message;
    private readonly UIButton _createButton;
    private readonly UIButton _backButton;

    /// <summary>How far down the list the visible rows start.</summary>
    private int _scrollOffset;

    /// <summary>The world whose row was last pressed, which is what the action just reported applies to.</summary>
    public string SelectedWorld { get; private set; } = string.Empty;

    /// <summary>Rows carry a name, a rename and a delete, so they need more width than a plain menu row.</summary>
    protected override float MaxRowWidth => 640;

    public UICanvasWorldList(Game game)
        : base(game, "Singleplayer", _backdropColor, 1.0F)
    {
        for (int row = 0; row < MaxVisibleRows; row++)
        {
            _playButtons[row] = new UIButton(this, string.Empty);
            _renameButtons[row] = new UIButton(this, RenameLabel);
            _deleteButtons[row] = new UIButton(this, DeleteLabel) { IsDestructive = true };
        }

        _message = new UIText(this, Font, Vector2.Zero, new Vector2(MessageScale, MessageScale), string.Empty)
        {
            Color = _messageColor,
        };
        AddComponentToRender(_message);

        _createButton = new UIButton(this, "Create New World");
        _backButton = new UIButton(this, "Back");

        Layout();
    }

    /// <summary>
    /// Hands the screen the worlds to show. Called every time it is opened, since one of them may have been
    /// renamed, deleted or created since the last look.
    /// </summary>
    public void SetWorlds(IReadOnlyList<string> worlds)
    {
        _worlds.Clear();
        _worlds.AddRange(worlds);

        ClampScroll();
        Layout();
    }

    public override void OnShown()
    {
        base.OnShown();
        ClampScroll();
    }

    public override MenuAction HandleInput(Vector2 mousePosition, bool mousePressed)
    {
        UpdateScrolling();

        MenuAction rowAction = UpdateRows(mousePosition, mousePressed);
        bool createPressed = _createButton.Update(mousePosition, mousePressed);
        bool backPressed = _backButton.Update(mousePosition, mousePressed);

        if (rowAction != MenuAction.None)
        {
            return rowAction;
        }

        if (createPressed)
        {
            return MenuAction.CreateWorld;
        }

        return backPressed ? MenuAction.Back : MenuAction.None;
    }

    private MenuAction UpdateRows(Vector2 mousePosition, bool mousePressed)
    {
        var action = MenuAction.None;

        for (int row = 0; row < MaxVisibleRows; row++)
        {
            // Every row is still updated, so that one scrolled out of sight does not keep the highlight it
            // had when the mouse was last over it.
            bool play = _playButtons[row].Update(mousePosition, mousePressed);
            bool rename = _renameButtons[row].Update(mousePosition, mousePressed);
            bool delete = _deleteButtons[row].Update(mousePosition, mousePressed);

            int worldIndex = _scrollOffset + row;
            if (!_playButtons[row].IsVisible || worldIndex >= _worlds.Count)
            {
                continue;
            }

            if (play || rename || delete)
            {
                SelectedWorld = _worlds[worldIndex];
                action = play ? MenuAction.PlaySelected
                    : rename ? MenuAction.RenameSelected
                    : MenuAction.DeleteSelected;
            }
        }

        return action;
    }

    private void UpdateScrolling()
    {
        int maxScroll = MaxScroll();
        if (maxScroll == 0)
        {
            return;
        }

        float wheel = Game.Input.ScrollDelta.Y;
        if (wheel == 0)
        {
            return;
        }

        // The wheel turning away from the player walks up the list, which is the way round a page scrolls.
        int moved = Math.Clamp(_scrollOffset - (wheel > 0 ? LinesPerScrollStep : -LinesPerScrollStep), 0, maxScroll);
        if (moved == _scrollOffset)
        {
            return;
        }

        _scrollOffset = moved;
        Layout();
    }

    private int MaxScroll() => Math.Max(0, _worlds.Count - MaxVisibleRows);

    private void ClampScroll() => _scrollOffset = Math.Clamp(_scrollOffset, 0, MaxScroll());

    protected override void Layout()
    {
        int rowsShown = Math.Clamp(_worlds.Count, 0, MaxVisibleRows);

        // A list that fits on screen has nothing to say about itself, and an empty line would only leave a
        // gap that looks like something failed to draw.
        string message = GetMessage(rowsShown);

        // Each block is what walking past it costs, gap included, so the same numbers place the rows below
        // and decide how tall the screen is. An empty list costs nothing rather than an empty row.
        float listBlock = rowsShown * (UIButton.Height + RowGap);

        // Measured to where the glyphs really end rather than to the nominal line height, which they hang
        // below far enough to reach into the button underneath. With nothing to say, the trailing gap is
        // kept on its own, so that the list never runs straight into the buttons below it.
        float messageBlock = message.Length == 0
            ? UIButton.Gap
            : Font.MeasureVerticalBounds(message, MessageScale).Bottom + UIButton.Gap;

        float contentHeight =
            listBlock + messageBlock +
            UIButton.Height + UIButton.Gap +
            UIButton.Height;

        float contentTop = Math.Max(110, (PixelHeight - contentHeight) / 2.0F);
        float top = contentTop;

        // Measured from the font rather than guessed at, so the two labels are never trimmed by a row that
        // was a few pixels short of holding them. On a window too narrow for that, the cap wins and the
        // labels trim themselves instead.
        float sideWidth = Math.Min(
            Math.Max(
                UIButton.MeasureRequiredWidth(Font, RenameLabel),
                UIButton.MeasureRequiredWidth(Font, DeleteLabel)),
            RowWidth / 3.5F);
        float playWidth = RowWidth - (2 * (sideWidth + RowGap));

        for (int row = 0; row < MaxVisibleRows; row++)
        {
            int worldIndex = _scrollOffset + row;
            bool isUsed = row < rowsShown && worldIndex < _worlds.Count;

            _playButtons[row].IsVisible = isUsed;
            _renameButtons[row].IsVisible = isUsed;
            _deleteButtons[row].IsVisible = isUsed;

            if (!isUsed)
            {
                continue;
            }

            _playButtons[row].Text = _worlds[worldIndex];
            _playButtons[row].SetBounds(new Vector2(RowLeft, top), new Vector2(playWidth, UIButton.Height));

            float renameLeft = RowLeft + playWidth + RowGap;
            _renameButtons[row].SetBounds(new Vector2(renameLeft, top), new Vector2(sideWidth, UIButton.Height));
            _deleteButtons[row].SetBounds(
                new Vector2(renameLeft + sideWidth + RowGap, top),
                new Vector2(sideWidth, UIButton.Height));

            top += UIButton.Height + RowGap;
        }

        _message.Text = message;
        _message.PixelPositionInCanvas = new Vector2(CenteredTextLeft(message, MessageScale), top);
        top += messageBlock;

        var wideRow = new Vector2(RowWidth, UIButton.Height);
        _createButton.SetBounds(new Vector2(RowLeft, top), wideRow);
        top += UIButton.Height + UIButton.Gap;

        _backButton.SetBounds(new Vector2(RowLeft, top), wideRow);
        top += UIButton.Height;

        LayoutFrame(contentTop, top + 20);
    }

    private string GetMessage(int rowsShown)
    {
        if (_worlds.Count == 0)
        {
            return "No worlds saved yet.";
        }

        if (MaxScroll() == 0)
        {
            return string.Empty;
        }

        int first = _scrollOffset + 1;
        int last = _scrollOffset + rowsShown;

        return first.ToString(CultureInfo.InvariantCulture) + "-" + last.ToString(CultureInfo.InvariantCulture) +
               " of " + _worlds.Count.ToString(CultureInfo.InvariantCulture) + ", scroll for more";
    }
}

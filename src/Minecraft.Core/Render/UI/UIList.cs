using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI;

public readonly record struct UIListItem(string Title, string Detail);

public sealed class UIList : UIWidget
{
    public const float RowHeight = 66F;
    public const float RowGap = 4F;
    public const float Inset = 6F;

    private const float AccentWidth = 4F;
    private const float TextInset = 16F;
    private const float ScrollbarWidth = 4F;
    private const float LineGap = 12F;

    private static readonly Vector3 _selectedColor = new(0.19F, 0.25F, 0.19F);

    private readonly UIPanel _frame;
    private readonly Row[] _rows;
    private readonly UIImage _scrollTrack;
    private readonly UIImage _scrollThumb;
    private readonly UILabel _emptyTitle;
    private readonly UILabel _emptyDetail;

    private readonly HeldKey _upKey = new(Keys.Up);
    private readonly HeldKey _downKey = new(Keys.Down);

    private readonly List<UIListItem> _items = [];

    private int _selectedIndex = -1;
    private int _scrollOffset;
    private int _hoveredRow = -1;

    public UIList(UICanvas canvas, int maxVisibleRows)
        : base(canvas)
    {
        _frame = new UIPanel(canvas, shadow: false, fill: UITheme.Field, border: UITheme.FieldBorder, fillTransparency: 0.85F);

        _rows = new Row[maxVisibleRows];
        for (int row = 0; row < maxVisibleRows; row++)
        {
            _rows[row] = new Row(canvas);
        }

        _scrollTrack = UIDraw.Rect(canvas, UITheme.Surface, 0.6F);
        _scrollThumb = UIDraw.Rect(canvas, UITheme.TextMuted);

        _emptyTitle = new UILabel(canvas, UITheme.HeadingScale, UITheme.TextMuted);
        _emptyDetail = new UILabel(canvas, UITheme.BodyScale, UITheme.TextFaint);
    }

    public int Count => _items.Count;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _selectedIndex = _items.Count == 0 ? -1 : Math.Clamp(value, 0, _items.Count - 1);
            EnsureSelectionVisible();
            Refresh();
        }
    }

    public bool WasActivated { get; private set; }

    public string EmptyTitle { get; set; } = "Nothing here";

    public string EmptyDetail { get; set; } = string.Empty;

    public int VisibleRows => Math.Clamp((int)((Size.Y - (2 * Inset) + RowGap) / (RowHeight + RowGap)), 1, _rows.Length);

    public override bool CanFocus => base.CanFocus && _items.Count > 0;

    public override bool WantsVerticalKeys => true;

    public static float HeightFor(int rows) => (2 * Inset) + (rows * RowHeight) + (Math.Max(0, rows - 1) * RowGap);

    public static int ClampScroll(int scrollOffset, int selectedIndex, int visibleRows, int count)
    {
        int maxScroll = Math.Max(0, count - visibleRows);
        int scroll = Math.Clamp(scrollOffset, 0, maxScroll);

        if (selectedIndex >= 0)
        {
            if (selectedIndex < scroll)
            {
                scroll = selectedIndex;
            }
            else if (selectedIndex >= scroll + visibleRows)
            {
                scroll = selectedIndex - visibleRows + 1;
            }
        }

        return Math.Clamp(scroll, 0, maxScroll);
    }

    public void SetItems(IReadOnlyList<UIListItem> items, int selectedIndex)
    {
        _items.Clear();
        _items.AddRange(items);
        _scrollOffset = 0;
        SelectedIndex = selectedIndex;
    }

    public override bool TryKeyboardActivate()
    {
        if (_selectedIndex < 0)
        {
            return false;
        }

        return base.TryKeyboardActivate();
    }

    public override bool Update(UIInput input)
    {
        int before = _selectedIndex;
        WasActivated = ConsumeActivation();

        UpdateMouse(input);

        if (IsFocused && _items.Count > 0)
        {
            UpdateKeys();
        }

        PaintRows();
        return _selectedIndex != before;
    }

    protected override void OnLayout() => Refresh();

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _frame.IsVisible = isVisible;
        Refresh();
    }

    private void UpdateMouse(UIInput input)
    {
        _hoveredRow = -1;

        if (!IsVisible)
        {
            return;
        }

        if (input.Scroll != 0F && Contains(input.Mouse))
        {
            int scrolled = Math.Clamp(_scrollOffset - Math.Sign(input.Scroll), 0, Math.Max(0, _items.Count - VisibleRows));
            if (scrolled != _scrollOffset)
            {
                _scrollOffset = scrolled;
                Refresh();
            }
        }

        for (int row = 0; row < VisibleRows; row++)
        {
            int index = _scrollOffset + row;
            if (index < _items.Count && _rows[row].Contains(input.Mouse))
            {
                _hoveredRow = row;

                if (input.Pressed)
                {
                    bool sameAsBefore = index == _selectedIndex;
                    SelectedIndex = index;
                    WasActivated |= input.DoubleClicked && sameAsBefore;
                }

                break;
            }
        }
    }

    private void UpdateKeys()
    {
        int target = _selectedIndex;

        if (_upKey.HasFired())
        {
            target = Math.Max(0, target - 1);
        }

        if (_downKey.HasFired())
        {
            target = Math.Min(_items.Count - 1, target + 1);
        }

        if (Game.Input.OnKeyPress(Keys.Home))
        {
            target = 0;
        }

        if (Game.Input.OnKeyPress(Keys.End))
        {
            target = _items.Count - 1;
        }

        if (Game.Input.OnKeyPress(Keys.PageUp))
        {
            target = Math.Max(0, target - VisibleRows);
        }

        if (Game.Input.OnKeyPress(Keys.PageDown))
        {
            target = Math.Min(_items.Count - 1, target + VisibleRows);
        }

        if (target != _selectedIndex)
        {
            SelectedIndex = target;
        }
    }

    private void EnsureSelectionVisible()
    {
        _scrollOffset = ClampScroll(_scrollOffset, _selectedIndex, VisibleRows, _items.Count);
    }

    private void Refresh()
    {
        _frame.SetBounds(Position, Size);
        _frame.BorderColor = FocusRingVisible ? UITheme.Focus : UITheme.FieldBorder;

        bool scrollable = _items.Count > VisibleRows;
        float rowWidth = Size.X - (2 * Inset) - (scrollable ? ScrollbarWidth + Inset : 0F);

        for (int row = 0; row < _rows.Length; row++)
        {
            int index = _scrollOffset + row;
            bool used = IsVisible && row < VisibleRows && index < _items.Count;
            _rows[row].IsVisible = used;

            if (!used)
            {
                continue;
            }

            var position = new Vector2(Position.X + Inset, Position.Y + Inset + (row * (RowHeight + RowGap)));
            _rows[row].Layout(position, new Vector2(rowWidth, RowHeight), _items[index]);
        }

        _scrollTrack.IsVisible = IsVisible && scrollable;
        _scrollThumb.IsVisible = IsVisible && scrollable;

        if (scrollable)
        {
            float trackLeft = Position.X + Size.X - Inset - ScrollbarWidth;
            float trackTop = Position.Y + Inset;
            float trackHeight = Size.Y - (2 * Inset);
            int maxScroll = _items.Count - VisibleRows;

            float thumbHeight = Math.Max(20F, trackHeight * VisibleRows / _items.Count);
            float thumbTop = trackTop + ((trackHeight - thumbHeight) * _scrollOffset / maxScroll);

            UIDraw.Place(_scrollTrack, trackLeft, trackTop, ScrollbarWidth, trackHeight);
            UIDraw.Place(_scrollThumb, trackLeft, thumbTop, ScrollbarWidth, thumbHeight);
        }

        bool empty = IsVisible && _items.Count == 0;
        _emptyTitle.IsVisible = empty;
        _emptyDetail.IsVisible = empty;

        if (empty)
        {
            _emptyTitle.Text = EmptyTitle;
            _emptyDetail.SetWrapped(EmptyDetail, Size.X - (4 * Inset));

            float middle = Position.Y + (Size.Y / 2F);
            _emptyTitle.PlaceTop(Position.X + ((Size.X - _emptyTitle.Width) / 2F), middle - _emptyTitle.CapHeight - 8F);
            _emptyDetail.PlaceTop(Position.X + ((Size.X - _emptyDetail.Width) / 2F), middle + 8F);
        }

        PaintRows();
    }

    private void PaintRows()
    {
        _frame.BorderColor = FocusRingVisible ? UITheme.Focus : UITheme.FieldBorder;

        for (int row = 0; row < _rows.Length; row++)
        {
            int index = _scrollOffset + row;
            _rows[row].Paint(index == _selectedIndex, row == _hoveredRow);
        }
    }

    private sealed class Row
    {
        private readonly UIImage _background;
        private readonly UIImage _accent;
        private readonly UILabel _title;
        private readonly UILabel _detail;

        private Vector2 _position;
        private Vector2 _size;

        public Row(UICanvas canvas)
        {
            _background = UIDraw.Rect(canvas, UITheme.Surface, 0F);
            _accent = UIDraw.Rect(canvas, UITheme.Accent);
            _title = new UILabel(canvas, UITheme.LabelScale, UITheme.Text);
            _detail = new UILabel(canvas, UITheme.SmallScale, UITheme.TextMuted, string.Empty, shadow: false);
        }

        public bool IsVisible
        {
            set
            {
                _background.IsVisible = value;
                _accent.IsVisible = value;
                _title.IsVisible = value;
                _detail.IsVisible = value;
            }
        }

        public bool Contains(Vector2 point)
        {
            return point.X >= _position.X && point.X <= _position.X + _size.X &&
                   point.Y >= _position.Y && point.Y <= _position.Y + _size.Y;
        }

        public void Layout(Vector2 position, Vector2 size, UIListItem item)
        {
            _position = position;
            _size = size;

            UIDraw.Place(_background, position, size);
            UIDraw.Place(_accent, position, new Vector2(AccentWidth, size.Y));

            float textWidth = size.X - (2 * TextInset);
            _title.SetFitted(item.Title, textWidth);
            _detail.SetFitted(item.Detail, textWidth);

            float block = _title.CapHeight + LineGap + _detail.CapHeight;
            float top = position.Y + MathF.Round((size.Y - block) / 2F);
            _title.PlaceTop(position.X + TextInset, top);
            _detail.PlaceTop(position.X + TextInset, top + _title.CapHeight + LineGap);
        }

        public void Paint(bool selected, bool hovered)
        {
            _accent.Transparency = selected ? 1F : 0F;
            _background.Color = selected ? _selectedColor : UITheme.Surface;
            _background.Transparency = selected ? 0.95F : hovered ? 0.45F : 0.18F;
            _detail.Color = selected ? UITheme.Text * 0.85F : UITheme.TextMuted;
        }
    }
}

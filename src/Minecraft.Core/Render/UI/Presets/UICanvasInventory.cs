using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// The inventory screen: the three rows the player is carrying, the same nine hotbar slots at the bottom that
/// the bar on the world screen shows, and — in creative only — every block in the game across the top.
/// <para>
/// That top section is a supply rather than a container: it hands out whole stacks of anything, and it is
/// also where a stack goes to be thrown away, since clicking it with a full cursor empties the cursor. In
/// survival there is nothing for it to be. Blocks come out of the ground there, so the list would be a way of
/// helping yourself to the things the mode is about earning, and it is left off the screen entirely.
/// </para>
/// </summary>
public sealed class UICanvasInventory : UICanvas
{
    private const float SlotSize = 46F;
    private const float SlotGap = 4F;

    /// <summary>Clear space between the edge of the panel and what is on it.</summary>
    private const float PanelPadding = 20F;

    /// <summary>The gap between one block of slots and the heading of the next.</summary>
    private const float SectionGap = 22F;

    /// <summary>The narrower gap between the storage rows and the hotbar, which read as one thing.</summary>
    private const float HotbarGap = 12F;

    private const float HeadingScale = 0.32F;
    private const float HeadingGap = 8F;
    private const float TitleScale = 0.44F;
    private const float HoveredNameScale = 0.34F;

    /// <summary>How far the world behind the screen is dimmed, rather than covered.</summary>
    private const float DimTransparency = 0.55F;

    private const float PanelTransparency = 0.94F;

    private static readonly Vector3 _dimColor = new(0.0F, 0.0F, 0.0F);
    private static readonly Vector3 _panelColor = new(0.09F, 0.10F, 0.12F);
    private static readonly Vector3 _headingColor = new(0.66F, 0.70F, 0.78F);
    private static readonly Vector3 _titleColor = new(0.95F, 0.95F, 0.95F);

    private readonly Game _game;
    private readonly Font _font;

    private readonly UIImage _dim;
    private readonly UIImage _panel;
    private readonly UIText _title;
    private readonly UIText _blocksHeading;
    private readonly UIText _carriedHeading;
    private readonly UIText _hoveredName;
    private readonly UIText _cursorCount;

    private readonly UISlotGrid _blocks;
    private readonly UISlotGrid _storage;
    private readonly UISlotGrid _hotbar;

    /// <summary>Where the name of the hovered block sits, worked out by the layout and centred on demand.</summary>
    private float _hoveredNameTop;

    /// <summary>
    /// Whether the block list is on the screen, which is the one thing about this layout that changes while
    /// the game is running: <c>/gamemode</c> moves it, and the panel has to be measured again when it does.
    /// </summary>
    private bool _showsBlockList = true;

    public UIOverlayCanvas Overlay { get; }

    public override bool IsEnabled
    {
        get => base.IsEnabled;
        set
        {
            base.IsEnabled = value;
            Overlay.IsEnabled = value;
        }
    }

    public UICanvasInventory(Game game)
        : base(
            Vector3.Zero,
            Vector3.Zero,
            game.Window.ClientSize.X,
            game.Window.ClientSize.Y,
            RenderSpace.Screen)
    {
        _game = game;
        _font = FontRegistry.GetFont(FontType.Arial);

        Overlay = new UIOverlayCanvas(PixelWidth, PixelHeight);

        _dim = new UIImage(this, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = _dimColor,
            Transparency = DimTransparency,
        };
        AddComponentToRender(_dim);

        _panel = new UIImage(this, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = _panelColor,
            Transparency = PanelTransparency,
        };
        AddComponentToRender(_panel);

        _title = AddLabel("Inventory", TitleScale, _titleColor);
        _blocksHeading = AddLabel("Blocks", HeadingScale, _headingColor);
        _carriedHeading = AddLabel("Carried", HeadingScale, _headingColor);

        _blocks = new UISlotGrid(this, Overlay, BlockCatalogue.Count, BlockCatalogue.Columns, SlotSize, SlotGap);
        _storage = new UISlotGrid(this, Overlay, Inventory.StorageSlots, Inventory.HotbarSlots, SlotSize, SlotGap);
        _hotbar = new UISlotGrid(this, Overlay, Inventory.HotbarSlots, Inventory.HotbarSlots, SlotSize, SlotGap);

        // On the overlay, so that the name of what is being pointed at and the count on the cursor are read
        // over the blocks rather than under them.
        _hoveredName = new UIText(
            Overlay,
            _font,
            Vector2.Zero,
            new Vector2(HoveredNameScale, HoveredNameScale),
            string.Empty)
        {
            Color = _titleColor,
        };
        Overlay.AddComponentToRender(_hoveredName);

        _cursorCount = new UIText(Overlay, _font, Vector2.Zero, new Vector2(0.26F, 0.26F), string.Empty)
        {
            Color = _titleColor,
        };
        Overlay.AddComponentToRender(_cursorCount);

        LayOut();
    }

    private UIText AddLabel(string text, float scale, Vector3 color)
    {
        var label = new UIText(this, _font, Vector2.Zero, new Vector2(scale, scale), text)
        {
            Color = color,
        };

        AddComponentToRender(label);
        return label;
    }

    public override void Update()
    {
        Inventory inventory = _game.ClientPlayer.Inventory;
        Vector2 mouse = Game.Input.MousePosition;

        // The one thing about this screen that can change without the window being resized.
        if (_showsBlockList != inventory.HasEndlessSupply)
        {
            _showsBlockList = inventory.HasEndlessSupply;
            LayOut();
        }

        int hoveredBlock = _showsBlockList ? _blocks.IndexAt(mouse) : -1;
        int hoveredStorage = _storage.IndexAt(mouse);
        int hoveredHotbar = _hotbar.IndexAt(mouse);

        HandleClicks(inventory, hoveredBlock, hoveredStorage, hoveredHotbar);

        if (_showsBlockList)
        {
            _blocks.Refresh(
                _game.MasterRenderer.BlockIcons,
                index => new ItemStack(BlockCatalogue.BlockAt(index), 1),
                hoveredBlock);
        }

        _storage.Refresh(
            _game.MasterRenderer.BlockIcons,
            index => inventory.GetSlot(Inventory.HotbarSlots + index),
            hoveredStorage);

        _hotbar.Refresh(_game.MasterRenderer.BlockIcons, inventory.GetSlot, hoveredHotbar);

        UpdateHoveredName(inventory, hoveredBlock, hoveredStorage, hoveredHotbar);
        UpdateCursorStack(inventory, mouse);

        Overlay.Clean();
    }

    private void HandleClicks(Inventory inventory, int hoveredBlock, int hoveredStorage, int hoveredHotbar)
    {
        // A click while the window is not focused is the click that focused it, and should not also move
        // whatever happened to be under the cursor.
        if (!_game.Window.IsFocused)
        {
            return;
        }

        bool left = Game.Input.OnMousePress(MouseButton.Left);
        bool right = Game.Input.OnMousePress(MouseButton.Right);

        if (!left && !right)
        {
            return;
        }

        if (hoveredBlock >= 0)
        {
            // The list is both where a stack comes from and where one goes: with something already on the
            // cursor there is nothing sensible to take, so this is what throws it away.
            if (!inventory.CursorStack.IsEmpty)
            {
                inventory.DiscardCursorStack();
                return;
            }

            inventory.TakeFromSupply(BlockCatalogue.BlockAt(hoveredBlock), right ? 1 : ItemStack.MaxCount);
            return;
        }

        if (hoveredStorage >= 0)
        {
            inventory.ClickSlot(Inventory.HotbarSlots + hoveredStorage, right);
            return;
        }

        if (hoveredHotbar >= 0)
        {
            inventory.ClickSlot(hoveredHotbar, right);
        }
    }

    /// <summary>Names whatever the cursor is over, under the slots, where a tooltip would otherwise go.</summary>
    private void UpdateHoveredName(Inventory inventory, int hoveredBlock, int hoveredStorage, int hoveredHotbar)
    {
        Block? block = null;

        if (hoveredBlock >= 0)
        {
            block = BlockCatalogue.BlockAt(hoveredBlock);
        }
        else if (hoveredStorage >= 0)
        {
            block = inventory.GetSlot(Inventory.HotbarSlots + hoveredStorage).Block;
        }
        else if (hoveredHotbar >= 0)
        {
            block = inventory.GetSlot(hoveredHotbar).Block;
        }

        string name = block is null ? string.Empty : BlockCatalogue.NameOf(block);
        if (name == _hoveredName.Text)
        {
            return;
        }

        _hoveredName.Text = name;
        CentreHoveredName();
    }

    /// <summary>
    /// Draws the stack being carried on the cursor, following the mouse. Queued after every slot, so it is
    /// drawn in front of whichever one it happens to be passing over.
    /// </summary>
    private void UpdateCursorStack(Inventory inventory, Vector2 mouse)
    {
        ItemStack cursor = inventory.CursorStack;

        if (cursor.IsEmpty)
        {
            _cursorCount.Text = string.Empty;
            return;
        }

        _game.MasterRenderer.BlockIcons.Queue(cursor.Block!, mouse, SlotSize * 0.78F);

        string count = cursor.Count > 1 ? cursor.Count.ToString() : string.Empty;
        _cursorCount.Text = count;

        if (count.Length > 0)
        {
            _cursorCount.PixelPositionInCanvas = mouse + new Vector2(SlotSize / 2F - 14F, SlotSize / 2F - 20F);
        }
    }

    protected override void OnDimensionsChanged()
    {
        Overlay.SetDimensions(PixelWidth, PixelHeight);
        LayOut();
    }

    /// <summary>
    /// How tall a run of text actually draws. Measured off the glyphs rather than taken from the font's own
    /// line height, which is the tallest character in it and leaves every heading floating in a gap.
    /// </summary>
    private float InkHeight(string text, float scale)
    {
        (float top, float bottom) = _font.MeasureVerticalBounds(text, scale);
        return bottom - top;
    }

    /// <summary>Places a label so that the top of its glyphs, rather than of its box, lands where asked.</summary>
    private void PlaceLabel(UIText label, float scale, float left, float top)
    {
        (float inkTop, _) = _font.MeasureVerticalBounds(label.Text, scale);
        label.PixelPositionInCanvas = new Vector2(left, top - inkTop);
    }

    private void LayOut()
    {
        _blocks.SetVisible(_showsBlockList);
        _blocksHeading.IsVisible = _showsBlockList;

        float headingHeight = InkHeight(_blocksHeading.Text, HeadingScale);
        float titleHeight = InkHeight(_title.Text, TitleScale);

        // Reserved against a string with both an ascender and a descender, so the row does not change height
        // with whichever block happens to be under the cursor.
        float nameHeight = InkHeight("Ag", HoveredNameScale);

        // The block list is the widest thing on the screen, so a survival panel is measured on the carried
        // rows instead and comes out narrower as well as shorter rather than opening onto empty space.
        float contentWidth = _showsBlockList ? _blocks.Width : _storage.Width;
        float blockListHeight = _showsBlockList
            ? headingHeight + HeadingGap + _blocks.Height + SectionGap
            : 0F;

        float contentHeight =
            titleHeight + SectionGap
            + blockListHeight
            + headingHeight + HeadingGap + _storage.Height + HotbarGap
            + _hotbar.Height + SectionGap + nameHeight;

        float panelWidth = contentWidth + (2 * PanelPadding);
        float panelHeight = contentHeight + (2 * PanelPadding);

        float panelLeft = (PixelWidth - panelWidth) / 2F;
        float panelTop = (PixelHeight - panelHeight) / 2F;

        _dim.PixelPositionInCanvas = Vector2.Zero;
        _dim.Dimension = new Vector2(PixelWidth, PixelHeight);

        _panel.PixelPositionInCanvas = new Vector2(panelLeft, panelTop);
        _panel.Dimension = new Vector2(panelWidth, panelHeight);

        float left = panelLeft + PanelPadding;
        float cursor = panelTop + PanelPadding;

        PlaceLabel(_title, TitleScale, left, cursor);
        cursor += titleHeight + SectionGap;

        if (_showsBlockList)
        {
            PlaceLabel(_blocksHeading, HeadingScale, left, cursor);
            cursor += headingHeight + HeadingGap;

            _blocks.SetOrigin(new Vector2(left, cursor));
            cursor += _blocks.Height + SectionGap;
        }

        PlaceLabel(_carriedHeading, HeadingScale, left, cursor);
        cursor += headingHeight + HeadingGap;

        _storage.SetOrigin(new Vector2(left, cursor));
        cursor += _storage.Height + HotbarGap;

        _hotbar.SetOrigin(new Vector2(left, cursor));
        cursor += _hotbar.Height + SectionGap;

        _hoveredNameTop = cursor;
        CentreHoveredName();
    }

    private void CentreHoveredName()
    {
        float width = _font.MeasureWidth(_hoveredName.Text, HoveredNameScale);
        PlaceLabel(_hoveredName, HoveredNameScale, (PixelWidth - width) / 2F, _hoveredNameTop);
    }
}

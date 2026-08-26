using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Crafting;
using Minecraft.Core.Inventories.Items;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// The inventory screen: a bench to lay a recipe out on, the three rows the player is carrying, the same nine
/// hotbar slots at the bottom that the bar on the world screen shows, and — in creative only — every block in
/// the game across the top.
/// <para>
/// That top section is a supply rather than a container: it hands out whole stacks of anything, and it is
/// also where a stack goes to be thrown away, since clicking it with a full cursor empties the cursor. In
/// survival there is nothing for it to be. Blocks come out of the ground there, so the list would be a way of
/// helping yourself to the things the mode is about earning, and it is left off the screen entirely.
/// </para>
/// <para>
/// One screen serves both benches. Opened with the inventory key it shows the two by two square carried
/// around in the inventory; opened by reaching for a crafting table it shows a three by three instead, and
/// everything else about it is the same. Two screens would have been the same layout written twice, differing
/// in one number.
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

    /// <summary>The gap between the bench and the slot holding what it makes.</summary>
    private const float ResultGap = 34F;

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
    private readonly UIText _craftingHeading;
    private readonly UIText _carriedHeading;
    private readonly UIText _hoveredName;
    private readonly UIText _cursorCount;

    private readonly UISlotGrid _blocks;
    private readonly UISlotGrid _storage;
    private readonly UISlotGrid _hotbar;

    // One block of slots per bench rather than one that grows: a grid is built with the number of slots it
    // has, and the two are shown and hidden as the screen is opened one way or the other.
    private readonly UISlotGrid _smallBench;
    private readonly UISlotGrid _largeBench;
    private readonly UISlotGrid _result;

    /// <summary>
    /// The bench belonging to whichever crafting table was last opened. It lives on the screen rather than on
    /// the block, because a table holds nothing: what is laid out on it is handed back the moment the screen
    /// closes, so there is never anything for a second player to find on it.
    /// </summary>
    private readonly CraftingGrid _tableGrid = new(3);

    /// <summary>Where the name of the hovered block sits, worked out by the layout and centred on demand.</summary>
    private float _hoveredNameTop;

    /// <summary>
    /// Whether the supply list is on the screen, which is one of the two things about this layout that change
    /// while the game is running: <c>/gamemode</c> moves it, and the panel has to be measured again when it
    /// does.
    /// </summary>
    private bool _showsBlockList = true;

    /// <summary>
    /// Which bench is on the screen: two when the inventory was opened on its own, three when it was opened
    /// by reaching for a crafting table. The other of the two things that move the layout.
    /// </summary>
    private int _benchSize = 2;

    /// <summary>The bench currently on the screen, which is what a click on one of its cells goes to.</summary>
    public CraftingGrid ActiveBench => _benchSize == 3 ? _tableGrid : _game.ClientPlayer.Inventory.Crafting;

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
        _blocksHeading = AddLabel("Everything", HeadingScale, _headingColor);
        _craftingHeading = AddLabel("Crafting", HeadingScale, _headingColor);
        _carriedHeading = AddLabel("Carried", HeadingScale, _headingColor);

        _blocks = new UISlotGrid(this, Overlay, ItemCatalogue.Count, ItemCatalogue.Columns, SlotSize, SlotGap);
        _smallBench = new UISlotGrid(this, Overlay, 4, 2, SlotSize, SlotGap);
        _largeBench = new UISlotGrid(this, Overlay, 9, 3, SlotSize, SlotGap);
        _result = new UISlotGrid(this, Overlay, 1, 1, SlotSize, SlotGap);
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

    /// <summary>
    /// Sets which bench the screen opens onto. Called as the screen is opened, so the layout is settled before
    /// the first frame of it is drawn.
    /// </summary>
    public void OpenWithBench(int benchSize)
    {
        if (_benchSize == benchSize)
        {
            return;
        }

        _benchSize = benchSize;
        LayOut();
    }

    /// <summary>
    /// Hands back whatever was laid out on the bench, called as the screen closes. Both benches are emptied
    /// and not only the one that was showing: a stack left on the table by a screen that was then reopened on
    /// the small bench is still a stack that belongs to somebody.
    /// </summary>
    public void ReturnBenchContents()
    {
        Inventory inventory = _game.ClientPlayer.Inventory;
        inventory.ReturnCraftingGrid(_tableGrid);
        inventory.ReturnCraftingGrid(inventory.Crafting);
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

        UISlotGrid bench = ActiveBenchGrid;
        CraftingGrid grid = ActiveBench;

        int hoveredBlock = _showsBlockList ? _blocks.IndexAt(mouse) : -1;
        int hoveredBench = bench.IndexAt(mouse);
        int hoveredResult = _result.IndexAt(mouse);
        int hoveredStorage = _storage.IndexAt(mouse);
        int hoveredHotbar = _hotbar.IndexAt(mouse);

        HandleClicks(inventory, grid, hoveredBlock, hoveredBench, hoveredResult, hoveredStorage, hoveredHotbar);

        ItemIconRenderer icons = _game.MasterRenderer.ItemIcons;

        if (_showsBlockList)
        {
            _blocks.Refresh(
                icons,
                index => new ItemStack(ItemCatalogue.ItemAt(index), 1),
                hoveredBlock);
        }

        bench.Refresh(icons, grid.GetSlot, hoveredBench);
        _result.Refresh(icons, _ => grid.Result, hoveredResult);

        _storage.Refresh(
            icons,
            index => inventory.GetSlot(Inventory.HotbarSlots + index),
            hoveredStorage);

        _hotbar.Refresh(icons, inventory.GetSlot, hoveredHotbar);

        UpdateHoveredName(inventory, grid, hoveredBlock, hoveredBench, hoveredResult, hoveredStorage, hoveredHotbar);
        UpdateCursorStack(inventory, mouse);

        Overlay.Clean();
    }

    /// <summary>The block of slots standing in for the bench currently on the screen.</summary>
    private UISlotGrid ActiveBenchGrid => _benchSize == 3 ? _largeBench : _smallBench;

    private void HandleClicks(
        Inventory inventory,
        CraftingGrid grid,
        int hoveredBlock,
        int hoveredBench,
        int hoveredResult,
        int hoveredStorage,
        int hoveredHotbar)
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

            inventory.TakeFromSupply(ItemCatalogue.ItemAt(hoveredBlock), right ? 1 : ItemStack.MaxCount);
            return;
        }

        if (hoveredBench >= 0)
        {
            inventory.ClickCraftingSlot(grid, hoveredBench, right);
            return;
        }

        if (hoveredResult >= 0)
        {
            // Taken whole or not at all, so the right button is the left button here. Half a pickaxe is not a
            // thing, and neither is two of a recipe that only made one.
            inventory.ClickCraftingResult(grid);
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
    private void UpdateHoveredName(
        Inventory inventory,
        CraftingGrid grid,
        int hoveredBlock,
        int hoveredBench,
        int hoveredResult,
        int hoveredStorage,
        int hoveredHotbar)
    {
        Item? item = null;

        if (hoveredBlock >= 0)
        {
            item = ItemCatalogue.ItemAt(hoveredBlock);
        }
        else if (hoveredBench >= 0)
        {
            item = grid.GetSlot(hoveredBench).Item;
        }
        else if (hoveredResult >= 0)
        {
            item = grid.Result.Item;
        }
        else if (hoveredStorage >= 0)
        {
            item = inventory.GetSlot(Inventory.HotbarSlots + hoveredStorage).Item;
        }
        else if (hoveredHotbar >= 0)
        {
            item = inventory.GetSlot(hoveredHotbar).Item;
        }

        string name = item?.Name ?? string.Empty;
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

        _game.MasterRenderer.ItemIcons.Queue(cursor, mouse, SlotSize * 0.78F);

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

        _smallBench.SetVisible(_benchSize == 2);
        _largeBench.SetVisible(_benchSize == 3);

        // The grid that has just been taken off the screen keeps whatever counts it was last refreshed with,
        // and those live on a canvas that is still being drawn.
        (_benchSize == 3 ? _smallBench : _largeBench).ClearCounts();

        float headingHeight = InkHeight(_blocksHeading.Text, HeadingScale);
        float titleHeight = InkHeight(_title.Text, TitleScale);

        // Reserved against a string with both an ascender and a descender, so the row does not change height
        // with whichever block happens to be under the cursor.
        float nameHeight = InkHeight("Ag", HoveredNameScale);

        // The supply list is the widest thing on the screen, so a survival panel is measured on the carried
        // rows instead and comes out narrower as well as shorter rather than opening onto empty space.
        float contentWidth = _showsBlockList ? _blocks.Width : _storage.Width;
        float blockListHeight = _showsBlockList
            ? headingHeight + HeadingGap + _blocks.Height + SectionGap
            : 0F;

        UISlotGrid bench = ActiveBenchGrid;

        float contentHeight =
            titleHeight + SectionGap
            + blockListHeight
            + headingHeight + HeadingGap + bench.Height + SectionGap
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

        PlaceLabel(_craftingHeading, HeadingScale, left, cursor);
        cursor += headingHeight + HeadingGap;

        bench.SetOrigin(new Vector2(left, cursor));

        // Set beside the bench and level with the middle of it, which is where the eye goes after laying a
        // recipe out and is far enough off that it is never mistaken for another cell of the bench.
        _result.SetOrigin(new Vector2(
            left + bench.Width + ResultGap,
            cursor + ((bench.Height - SlotSize) / 2F)));

        cursor += bench.Height + SectionGap;

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

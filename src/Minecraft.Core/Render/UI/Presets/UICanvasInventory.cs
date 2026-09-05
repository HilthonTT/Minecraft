using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Crafting;
using Minecraft.Core.Inventories.Items;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasInventory : UICanvas
{
    private const float SlotSize = 46F;
    private const float SlotGap = 4F;

    private const float PanelPadding = 20F;

    private const float SectionGap = 22F;

    private const float HotbarGap = 12F;

    private const float ResultGap = 34F;

    private const float HeadingScale = 0.32F;
    private const float HeadingGap = 8F;
    private const float TitleScale = 0.44F;
    private const float HoveredNameScale = 0.34F;

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

    private readonly UISlotGrid _smallBench;
    private readonly UISlotGrid _largeBench;
    private readonly UISlotGrid _result;

    private readonly CraftingGrid _tableGrid = new(3);

    private float _hoveredNameTop;

    private bool _showsBlockList = true;

    private int _benchSize = 2;

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

    public void OpenWithBench(int benchSize)
    {
        if (_benchSize == benchSize)
        {
            return;
        }

        _benchSize = benchSize;
        LayOut();
    }

    public List<ItemStack> ReturnBenchContents()
    {
        Inventory inventory = _game.ClientPlayer.Inventory;

        List<ItemStack> leftovers = inventory.ReturnCraftingGrid(_tableGrid);
        leftovers.AddRange(inventory.ReturnCraftingGrid(inventory.Crafting));
        return leftovers;
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

    private float InkHeight(string text, float scale)
    {
        (float top, float bottom) = _font.MeasureVerticalBounds(text, scale);
        return bottom - top;
    }

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

        (_benchSize == 3 ? _smallBench : _largeBench).ClearCounts();

        float headingHeight = InkHeight(_blocksHeading.Text, HeadingScale);
        float titleHeight = InkHeight(_title.Text, TitleScale);

        float nameHeight = InkHeight("Ag", HoveredNameScale);

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

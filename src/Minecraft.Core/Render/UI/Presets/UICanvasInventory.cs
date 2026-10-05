using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Crafting;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasInventory : UICanvas
{
    private enum ScreenMode
    {
        SmallBench,
        LargeBench,
        Chest,
        Furnace,
    }

    private const float SlotSize = 46F;
    private const float SlotGap = 4F;

    private const float PanelPadding = 20F;

    private const float SectionGap = 22F;

    private const float HotbarGap = 12F;

    private const float ResultGap = 34F;

    private const float FlameHeight = 12F;
    private const float FlameWidth = 14F;
    private const float FlameGap = 6F;

    private const float ArrowLength = 64F;
    private const float ArrowThickness = 8F;

    private const float FurnaceIndent = 2F * (SlotSize + SlotGap);

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
    private static readonly Vector3 _trackColor = new(0.15F, 0.16F, 0.19F);
    private static readonly Vector3 _flameColor = new(0.98F, 0.55F, 0.12F);
    private static readonly Vector3 _arrowColor = new(0.92F, 0.92F, 0.95F);

    private readonly Game _game;
    private readonly Font _font;

    private readonly UIImage _dim;
    private readonly UIImage _panel;
    private readonly UIText _title;
    private readonly UIText _blocksHeading;
    private readonly UIText _sectionHeading;
    private readonly UIText _carriedHeading;
    private readonly UIText _hoveredName;
    private readonly UIText _cursorCount;

    private readonly UISlotGrid _blocks;
    private readonly UISlotGrid _storage;
    private readonly UISlotGrid _hotbar;

    private readonly UISlotGrid _smallBench;
    private readonly UISlotGrid _largeBench;
    private readonly UISlotGrid _result;

    private readonly UISlotGrid _chest;

    private readonly UISlotGrid _furnaceInput;
    private readonly UISlotGrid _furnaceFuel;
    private readonly UISlotGrid _furnaceOutput;

    private readonly UIImage _flameTrack;
    private readonly UIImage _flameBar;
    private readonly UIImage _arrowTrack;
    private readonly UIImage _arrowBar;

    private readonly CraftingGrid _tableGrid = new(3);

    private float _hoveredNameTop;

    private bool _showsBlockList = true;

    private ScreenMode _mode = ScreenMode.SmallBench;

    private Vector3i _containerPos;

    public CraftingGrid ActiveBench => _mode == ScreenMode.LargeBench ? _tableGrid : _game.ClientPlayer.Inventory.Crafting;

    public Vector3i? OpenChestPos => _mode == ScreenMode.Chest ? _containerPos : null;

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
        _sectionHeading = AddLabel("Crafting", HeadingScale, _headingColor);
        _carriedHeading = AddLabel("Carried", HeadingScale, _headingColor);

        _blocks = new UISlotGrid(this, Overlay, ItemCatalogue.Count, ItemCatalogue.Columns, SlotSize, SlotGap);
        _smallBench = new UISlotGrid(this, Overlay, 4, 2, SlotSize, SlotGap);
        _largeBench = new UISlotGrid(this, Overlay, 9, 3, SlotSize, SlotGap);
        _result = new UISlotGrid(this, Overlay, 1, 1, SlotSize, SlotGap);
        _chest = new UISlotGrid(this, Overlay, BlockStateChest.Slots, Inventory.HotbarSlots, SlotSize, SlotGap);
        _furnaceInput = new UISlotGrid(this, Overlay, 1, 1, SlotSize, SlotGap);
        _furnaceFuel = new UISlotGrid(this, Overlay, 1, 1, SlotSize, SlotGap);
        _furnaceOutput = new UISlotGrid(this, Overlay, 1, 1, SlotSize, SlotGap);
        _storage = new UISlotGrid(this, Overlay, Inventory.StorageSlots, Inventory.HotbarSlots, SlotSize, SlotGap);
        _hotbar = new UISlotGrid(this, Overlay, Inventory.HotbarSlots, Inventory.HotbarSlots, SlotSize, SlotGap);

        _flameTrack = AddBar(_trackColor);
        _flameBar = AddBar(_flameColor);
        _arrowTrack = AddBar(_trackColor);
        _arrowBar = AddBar(_arrowColor);

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
        SetMode(benchSize == 3 ? ScreenMode.LargeBench : ScreenMode.SmallBench);
    }

    public void OpenWithContainer(Vector3i blockPos, BlockState state)
    {
        _containerPos = blockPos;
        SetMode(state is BlockStateFurnace ? ScreenMode.Furnace : ScreenMode.Chest);
    }

    private void SetMode(ScreenMode mode)
    {
        if (_mode == mode)
        {
            return;
        }

        _mode = mode;
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

    private UIImage AddBar(Vector3 color)
    {
        var bar = new UIImage(this, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = color,
            IsVisible = false,
        };

        AddComponentToRender(bar);
        return bar;
    }

    private bool IsBenchMode => _mode is ScreenMode.SmallBench or ScreenMode.LargeBench;

    private IContainerState? FindOpenContainer()
    {
        if (IsBenchMode)
        {
            return null;
        }

        BlockState state = _game.World.GetBlockAt(_containerPos);
        bool matches = _mode == ScreenMode.Furnace ? state is BlockStateFurnace : state is BlockStateChest;

        return matches ? (IContainerState)state : null;
    }

    public override void Update()
    {
        Inventory inventory = _game.ClientPlayer.Inventory;
        Vector2 mouse = Game.Input.MousePosition;

        IContainerState? container = FindOpenContainer();
        if (!IsBenchMode && container is null)
        {
            _game.CloseInventory();
            return;
        }

        if (_showsBlockList != inventory.HasEndlessSupply)
        {
            _showsBlockList = inventory.HasEndlessSupply;
            LayOut();
        }

        var hovered = new Hovered
        {
            Block = _showsBlockList ? _blocks.IndexAt(mouse) : -1,
            Bench = IsBenchMode ? ActiveBenchGrid.IndexAt(mouse) : -1,
            Result = IsBenchMode ? _result.IndexAt(mouse) : -1,
            ContainerSlot = ContainerSlotAt(mouse),
            Storage = _storage.IndexAt(mouse),
            Hotbar = _hotbar.IndexAt(mouse),
        };

        CraftingGrid grid = ActiveBench;

        HandleClicks(inventory, grid, container, hovered);

        ItemIconRenderer icons = _game.MasterRenderer.ItemIcons;

        if (_showsBlockList)
        {
            _blocks.Refresh(
                icons,
                index => new ItemStack(ItemCatalogue.ItemAt(index), 1),
                hovered.Block);
        }

        if (IsBenchMode)
        {
            ActiveBenchGrid.Refresh(icons, grid.GetSlot, hovered.Bench);
            _result.Refresh(icons, _ => grid.Result, hovered.Result);
        }
        else if (_mode == ScreenMode.Chest)
        {
            _chest.Refresh(icons, container!.GetSlot, hovered.ContainerSlot);
        }
        else
        {
            RefreshFurnace(icons, (BlockStateFurnace)container!, hovered.ContainerSlot);
        }

        _storage.Refresh(
            icons,
            index => inventory.GetSlot(Inventory.HotbarSlots + index),
            hovered.Storage);

        _hotbar.Refresh(icons, inventory.GetSlot, hovered.Hotbar);

        UpdateHoveredName(inventory, grid, container, hovered);
        UpdateCursorStack(inventory, mouse);

        Overlay.Clean();
    }

    private struct Hovered
    {
        public int Block;
        public int Bench;
        public int Result;
        public int ContainerSlot;
        public int Storage;
        public int Hotbar;
    }

    private int ContainerSlotAt(Vector2 mouse)
    {
        switch (_mode)
        {
            case ScreenMode.Chest:
                return _chest.IndexAt(mouse);

            case ScreenMode.Furnace:
                if (_furnaceInput.IndexAt(mouse) >= 0)
                {
                    return BlockStateFurnace.InputSlot;
                }

                if (_furnaceFuel.IndexAt(mouse) >= 0)
                {
                    return BlockStateFurnace.FuelSlot;
                }

                return _furnaceOutput.IndexAt(mouse) >= 0 ? BlockStateFurnace.OutputSlot : -1;

            default:
                return -1;
        }
    }

    private void RefreshFurnace(ItemIconRenderer icons, BlockStateFurnace furnace, int hoveredSlot)
    {
        _furnaceInput.Refresh(icons, _ => furnace.Input, hoveredSlot == BlockStateFurnace.InputSlot ? 0 : -1);
        _furnaceFuel.Refresh(icons, _ => furnace.Fuel, hoveredSlot == BlockStateFurnace.FuelSlot ? 0 : -1);
        _furnaceOutput.Refresh(icons, _ => furnace.Output, hoveredSlot == BlockStateFurnace.OutputSlot ? 0 : -1);

        float flame = furnace.BurnFraction;
        _flameBar.IsVisible = flame > 0F;
        _flameBar.Dimension = new Vector2(FlameWidth, FlameHeight * flame);
        _flameBar.PixelPositionInCanvas = _flameTrack.PixelPositionInCanvas + new Vector2(0F, FlameHeight * (1F - flame));

        float cooked = furnace.CookFraction;
        _arrowBar.IsVisible = cooked > 0F;
        _arrowBar.Dimension = new Vector2(ArrowLength * cooked, ArrowThickness);
        _arrowBar.PixelPositionInCanvas = _arrowTrack.PixelPositionInCanvas;
    }

    private UISlotGrid ActiveBenchGrid => _mode == ScreenMode.LargeBench ? _largeBench : _smallBench;

    private void HandleClicks(Inventory inventory, CraftingGrid grid, IContainerState? container, Hovered hovered)
    {
        if (!_game.Window.IsFocused || _game.ClientPlayer.HasPendingContainerClick)
        {
            return;
        }

        bool left = Game.Input.OnMousePress(MouseButton.Left);
        bool right = Game.Input.OnMousePress(MouseButton.Right);

        if (!left && !right)
        {
            return;
        }

        if (hovered.Block >= 0)
        {
            if (!inventory.CursorStack.IsEmpty)
            {
                inventory.DiscardCursorStack();
                return;
            }

            inventory.TakeFromSupply(ItemCatalogue.ItemAt(hovered.Block), right ? 1 : ItemStack.MaxCount);
            return;
        }

        if (hovered.Bench >= 0)
        {
            inventory.ClickCraftingSlot(grid, hovered.Bench, right);
            return;
        }

        if (hovered.Result >= 0)
        {
            inventory.ClickCraftingResult(grid);
            return;
        }

        if (hovered.ContainerSlot >= 0 && container is not null)
        {
            ClickContainerSlot(inventory, container, hovered.ContainerSlot, right);
            return;
        }

        if (hovered.Storage >= 0)
        {
            inventory.ClickSlot(Inventory.HotbarSlots + hovered.Storage, right);
            return;
        }

        if (hovered.Hotbar >= 0)
        {
            inventory.ClickSlot(hovered.Hotbar, right);
        }
    }

    private void ClickContainerSlot(Inventory inventory, IContainerState container, int slot, bool right)
    {
        ItemStack current = container.GetSlot(slot);
        ItemStack cursor = inventory.CursorStack;

        ItemStack updated = inventory.ClickContainerSlot(container, slot, right);

        if (updated.SameAs(current) && cursor.SameAs(inventory.CursorStack))
        {
            return;
        }

        container.SetSlot(slot, updated);

        _game.Client.WritePacket(new ContainerClickPacket(
            _containerPos,
            slot,
            right,
            cursor,
            _game.ClientPlayer.BeginContainerClick(_containerPos)));
    }

    private void UpdateHoveredName(Inventory inventory, CraftingGrid grid, IContainerState? container, Hovered hovered)
    {
        Item? item = null;

        if (hovered.Block >= 0)
        {
            item = ItemCatalogue.ItemAt(hovered.Block);
        }
        else if (hovered.Bench >= 0)
        {
            item = grid.GetSlot(hovered.Bench).Item;
        }
        else if (hovered.Result >= 0)
        {
            item = grid.Result.Item;
        }
        else if (hovered.ContainerSlot >= 0 && container is not null)
        {
            item = container.GetSlot(hovered.ContainerSlot).Item;
        }
        else if (hovered.Storage >= 0)
        {
            item = inventory.GetSlot(Inventory.HotbarSlots + hovered.Storage).Item;
        }
        else if (hovered.Hotbar >= 0)
        {
            item = inventory.GetSlot(hovered.Hotbar).Item;
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

    private string SectionHeadingText => _mode switch
    {
        ScreenMode.Chest => "Chest",
        ScreenMode.Furnace => "Furnace",
        _ => "Crafting",
    };

    private float SectionHeight => _mode switch
    {
        ScreenMode.Chest => _chest.Height,
        ScreenMode.Furnace => (2F * SlotSize) + FlameHeight + (2F * FlameGap),
        _ => ActiveBenchGrid.Height,
    };

    private void LayOut()
    {
        _blocks.SetVisible(_showsBlockList);
        _blocksHeading.IsVisible = _showsBlockList;

        _smallBench.SetVisible(_mode == ScreenMode.SmallBench);
        _largeBench.SetVisible(_mode == ScreenMode.LargeBench);
        _result.SetVisible(IsBenchMode);
        _chest.SetVisible(_mode == ScreenMode.Chest);

        bool furnace = _mode == ScreenMode.Furnace;
        _furnaceInput.SetVisible(furnace);
        _furnaceFuel.SetVisible(furnace);
        _furnaceOutput.SetVisible(furnace);
        _flameTrack.IsVisible = furnace;
        _arrowTrack.IsVisible = furnace;
        _flameBar.IsVisible = false;
        _arrowBar.IsVisible = false;

        foreach (UISlotGrid grid in new[] { _smallBench, _largeBench, _result, _chest, _furnaceInput, _furnaceFuel, _furnaceOutput })
        {
            grid.ClearCounts();
        }

        _sectionHeading.Text = SectionHeadingText;

        float headingHeight = InkHeight(_blocksHeading.Text, HeadingScale);
        float titleHeight = InkHeight(_title.Text, TitleScale);

        float nameHeight = InkHeight("Ag", HoveredNameScale);

        float contentWidth = _showsBlockList ? _blocks.Width : _storage.Width;
        float blockListHeight = _showsBlockList
            ? headingHeight + HeadingGap + _blocks.Height + SectionGap
            : 0F;

        float contentHeight =
            titleHeight + SectionGap
            + blockListHeight
            + headingHeight + HeadingGap + SectionHeight + SectionGap
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

        PlaceLabel(_sectionHeading, HeadingScale, left, cursor);
        cursor += headingHeight + HeadingGap;

        LayOutSection(left, cursor);

        cursor += SectionHeight + SectionGap;

        PlaceLabel(_carriedHeading, HeadingScale, left, cursor);
        cursor += headingHeight + HeadingGap;

        _storage.SetOrigin(new Vector2(left, cursor));
        cursor += _storage.Height + HotbarGap;

        _hotbar.SetOrigin(new Vector2(left, cursor));
        cursor += _hotbar.Height + SectionGap;

        _hoveredNameTop = cursor;
        CentreHoveredName();
    }

    private void LayOutSection(float left, float top)
    {
        switch (_mode)
        {
            case ScreenMode.Chest:
                _chest.SetOrigin(new Vector2(left, top));
                break;

            case ScreenMode.Furnace:
                LayOutFurnace(left + FurnaceIndent, top);
                break;

            default:
                UISlotGrid bench = ActiveBenchGrid;
                bench.SetOrigin(new Vector2(left, top));
                _result.SetOrigin(new Vector2(
                    left + bench.Width + ResultGap,
                    top + ((bench.Height - SlotSize) / 2F)));
                break;
        }
    }

    private void LayOutFurnace(float left, float top)
    {
        _furnaceInput.SetOrigin(new Vector2(left, top));

        float flameTop = top + SlotSize + FlameGap;
        _flameTrack.PixelPositionInCanvas = new Vector2(left + ((SlotSize - FlameWidth) / 2F), flameTop);
        _flameTrack.Dimension = new Vector2(FlameWidth, FlameHeight);

        _furnaceFuel.SetOrigin(new Vector2(left, flameTop + FlameHeight + FlameGap));

        float middle = top + (SectionHeight / 2F);
        float arrowLeft = left + SlotSize + ResultGap;

        _arrowTrack.PixelPositionInCanvas = new Vector2(arrowLeft, middle - (ArrowThickness / 2F));
        _arrowTrack.Dimension = new Vector2(ArrowLength, ArrowThickness);

        _furnaceOutput.SetOrigin(new Vector2(arrowLeft + ArrowLength + ResultGap, middle - (SlotSize / 2F)));
    }

    private void CentreHoveredName()
    {
        float width = _font.MeasureWidth(_hoveredName.Text, HoveredNameScale);
        PlaceLabel(_hoveredName, HoveredNameScale, (PixelWidth - width) / 2F, _hoveredNameTop);
    }
}

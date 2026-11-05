using System.Globalization;
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

    private const float PreferredSlotSize = 46F;
    private const float MinimumSlotSize = 26F;
    private const float SlotGapFraction = 4F / 46F;

    private const float PanelPadding = 20F;

    private const float SectionGap = 22F;

    private const float HotbarGap = 12F;

    private const float ResultGap = 34F;

    private const float FlameHeight = 12F;
    private const float FlameWidth = 14F;
    private const float FlameGap = 6F;

    private const float ArrowLength = 64F;
    private const float ArrowThickness = 8F;

    private const float HeadingScale = UITheme.SmallScale;
    private const float HeadingGap = 8F;
    private const float TitleScale = UITheme.HeadingScale;
    private const float HintScale = UITheme.TinyScale;

    private const float DimTransparency = 0.55F;

    private const string BenchHints = "Left click  Take / place all      Right click  Split / place one      E or Esc  Close";
    private const string SupplyHints = "Left click  Take / place all      Right click  Split / place one      Click the list  Throw away";

    private static readonly Vector3 _trackColor = new(0.15F, 0.16F, 0.19F);
    private static readonly Vector3 _flameColor = new(0.98F, 0.55F, 0.12F);
    private static readonly Vector3 _arrowColor = new(0.92F, 0.92F, 0.95F);

    private readonly Game _game;

    private readonly UIImage _dim;
    private readonly UIImage _vignette;
    private readonly UIPanel _panel;
    private readonly UILabel _title;
    private readonly UILabel _blocksHeading;
    private readonly UILabel _sectionHeading;
    private readonly UILabel _carriedHeading;
    private readonly UILabel _hints;
    private readonly UILabel _cursorCount;
    private readonly UITooltip _tooltip;

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

    private float _slotSize = PreferredSlotSize;

    private float _slotGap = PreferredSlotSize * SlotGapFraction;

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
        : base(Vector3.Zero, Vector3.Zero, UIScale.CanvasWidth, UIScale.CanvasHeight, RenderSpace.Screen)
    {
        _game = game;

        Overlay = new UIOverlayCanvas(PixelWidth, PixelHeight);

        _dim = UIDraw.Rect(this, UITheme.Dim, DimTransparency);
        _vignette = UIDraw.Rect(this, Vector3.One, 0.5F, UITextures.Vignette);
        _panel = new UIPanel(this);

        _title = AddLabel("Inventory", TitleScale, UITheme.Text);
        _blocksHeading = AddLabel("EVERYTHING", HeadingScale, UITheme.Accent);
        _sectionHeading = AddLabel("CRAFTING", HeadingScale, UITheme.Accent);
        _carriedHeading = AddLabel("CARRIED", HeadingScale, UITheme.Accent);
        _hints = new UILabel(this, HintScale, UITheme.TextFaint, string.Empty, shadow: false);

        float slotSize = PreferredSlotSize;
        float slotGap = _slotGap;

        _blocks = new UISlotGrid(this, Overlay, ItemCatalogue.Count, ItemCatalogue.Columns, slotSize, slotGap);
        _smallBench = new UISlotGrid(this, Overlay, 4, 2, slotSize, slotGap);
        _largeBench = new UISlotGrid(this, Overlay, 9, 3, slotSize, slotGap);
        _result = new UISlotGrid(this, Overlay, 1, 1, slotSize, slotGap);
        _chest = new UISlotGrid(this, Overlay, BlockStateChest.Slots, Inventory.HotbarSlots, slotSize, slotGap);
        _furnaceInput = new UISlotGrid(this, Overlay, 1, 1, slotSize, slotGap);
        _furnaceFuel = new UISlotGrid(this, Overlay, 1, 1, slotSize, slotGap);
        _furnaceOutput = new UISlotGrid(this, Overlay, 1, 1, slotSize, slotGap);
        _storage = new UISlotGrid(this, Overlay, Inventory.StorageSlots, Inventory.HotbarSlots, slotSize, slotGap);
        _hotbar = new UISlotGrid(this, Overlay, Inventory.HotbarSlots, Inventory.HotbarSlots, slotSize, slotGap);

        _flameTrack = AddBar(_trackColor);
        _flameBar = AddBar(_flameColor);
        _arrowTrack = AddBar(_trackColor);
        _arrowBar = AddBar(_arrowColor);

        _cursorCount = new UILabel(Overlay, 0.24F, UITheme.Text);
        _tooltip = new UITooltip(Overlay);

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

    private UILabel AddLabel(string text, float scale, Vector3 color)
    {
        return new UILabel(this, scale, color, text);
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
        Vector2 mouse = UIScale.ToCanvas(Game.Input.MousePosition);

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

        UpdateTooltip(inventory, grid, container, hovered, mouse);
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

    private void UpdateTooltip(
        Inventory inventory,
        CraftingGrid grid,
        IContainerState? container,
        Hovered hovered,
        Vector2 mouse)
    {
        if (!inventory.CursorStack.IsEmpty)
        {
            _tooltip.Hide();
            return;
        }

        ItemStack stack = ItemStack.Empty;
        string extra = string.Empty;

        if (hovered.Block >= 0)
        {
            stack = new ItemStack(ItemCatalogue.ItemAt(hovered.Block), 1);
            extra = "Left click  Take a stack\nRight click  Take one";
        }
        else if (hovered.Bench >= 0)
        {
            stack = grid.GetSlot(hovered.Bench);
        }
        else if (hovered.Result >= 0)
        {
            stack = grid.Result;
            extra = "Click to craft";
        }
        else if (hovered.ContainerSlot >= 0 && container is not null)
        {
            stack = container.GetSlot(hovered.ContainerSlot);
        }
        else if (hovered.Storage >= 0)
        {
            stack = inventory.GetSlot(Inventory.HotbarSlots + hovered.Storage);
        }
        else if (hovered.Hotbar >= 0)
        {
            stack = inventory.GetSlot(hovered.Hotbar);
        }

        if (stack.IsEmpty)
        {
            _tooltip.Hide();
            return;
        }

        string detail = UIItemDescriptions.Describe(stack, showCount: hovered.Block < 0);
        if (extra.Length > 0)
        {
            detail = detail.Length == 0 ? extra : detail + "\n" + extra;
        }

        _tooltip.Show(mouse, stack.Item!.Name, detail, UITheme.Text);
    }

    private void UpdateCursorStack(Inventory inventory, Vector2 mouse)
    {
        ItemStack cursor = inventory.CursorStack;

        if (cursor.IsEmpty)
        {
            _cursorCount.Text = string.Empty;
            return;
        }

        _game.MasterRenderer.ItemIcons.Queue(
            cursor,
            UIScale.ToWindow(mouse),
            UIScale.LengthToWindow(_slotSize * 0.8F));

        string count = cursor.Count > 1 ? cursor.Count.ToString(CultureInfo.InvariantCulture) : string.Empty;
        _cursorCount.Text = count;

        if (count.Length > 0)
        {
            _cursorCount.PlaceTop(
                mouse.X + (_slotSize * 0.42F) - _cursorCount.Width,
                mouse.Y + (_slotSize * 0.42F) - _cursorCount.CapHeight);
        }
    }

    protected override void OnDimensionsChanged()
    {
        Overlay.SetDimensions(PixelWidth, PixelHeight);
        LayOut();
    }

    public static float FitSlotSize(float preferred, float minimum, float naturalHeight, float slotRows, float availableHeight)
    {
        if (naturalHeight <= availableHeight || slotRows <= 0F)
        {
            return preferred;
        }

        float overflow = naturalHeight - availableHeight;
        float perSlot = overflow / (slotRows * (1F + SlotGapFraction));
        return Math.Max(minimum, MathF.Floor(preferred - perSlot));
    }

    private string SectionHeadingText => _mode switch
    {
        ScreenMode.Chest => "CHEST",
        ScreenMode.Furnace => "FURNACE",
        _ => "CRAFTING",
    };

    private float SectionHeight => _mode switch
    {
        ScreenMode.Chest => _chest.Height,
        ScreenMode.Furnace => (2F * _slotSize) + FlameHeight + (2F * FlameGap),
        _ => ActiveBenchGrid.Height,
    };

    private float SectionSlotRows => _mode switch
    {
        ScreenMode.Chest => _chest.Rows,
        ScreenMode.Furnace => 2F,
        _ => ActiveBenchGrid.Rows,
    };

    private IEnumerable<UISlotGrid> AllGrids =>
    [
        _blocks, _smallBench, _largeBench, _result, _chest, _furnaceInput, _furnaceFuel, _furnaceOutput, _storage, _hotbar,
    ];

    private float ContentHeight(float headingHeight, float titleHeight, float hintHeight)
    {
        float blockListHeight = _showsBlockList ? headingHeight + HeadingGap + _blocks.Height + SectionGap : 0F;

        return titleHeight + SectionGap
            + blockListHeight
            + headingHeight + HeadingGap + SectionHeight + SectionGap
            + headingHeight + HeadingGap + _storage.Height + HotbarGap
            + _hotbar.Height + SectionGap + hintHeight;
    }

    private void ApplySlotSize(float slotSize)
    {
        _slotSize = slotSize;
        _slotGap = MathF.Max(2F, MathF.Round(slotSize * SlotGapFraction));

        foreach (UISlotGrid grid in AllGrids)
        {
            grid.SetSlotSize(_slotSize, _slotGap);
        }
    }

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
        ApplySlotSize(PreferredSlotSize);
        WrapHints();

        float headingHeight = _blocksHeading.CapHeight;
        float titleHeight = _title.CapHeight;
        float hintHeight = _hints.BlockHeight;

        float naturalHeight = ContentHeight(headingHeight, titleHeight, hintHeight) + (2 * PanelPadding);
        float slotRows = (_showsBlockList ? _blocks.Rows : 0) + SectionSlotRows + _storage.Rows + _hotbar.Rows;
        float available = PixelHeight - (2 * UITheme.Margin);

        float fitted = FitSlotSize(PreferredSlotSize, MinimumSlotSize, naturalHeight, slotRows, available);
        if (fitted != PreferredSlotSize)
        {
            ApplySlotSize(fitted);
            WrapHints();
            hintHeight = _hints.BlockHeight;
        }

        float contentWidth = _showsBlockList ? _blocks.Width : _storage.Width;
        float contentHeight = ContentHeight(headingHeight, titleHeight, hintHeight);

        float panelWidth = contentWidth + (2 * PanelPadding);
        float panelHeight = contentHeight + (2 * PanelPadding);

        float panelLeft = MathF.Round((PixelWidth - panelWidth) / 2F);
        float panelTop = MathF.Round(Math.Max(UITheme.Margin / 2F, (PixelHeight - panelHeight) / 2F));

        UIDraw.Place(_dim, Vector2.Zero, new Vector2(PixelWidth, PixelHeight));
        UIDraw.Place(_vignette, Vector2.Zero, new Vector2(PixelWidth, PixelHeight));
        _panel.SetBounds(panelLeft, panelTop, panelWidth, panelHeight);

        float left = panelLeft + PanelPadding;
        float cursor = panelTop + PanelPadding;

        _title.PlaceTop(left, cursor);
        cursor += titleHeight + SectionGap;

        if (_showsBlockList)
        {
            _blocksHeading.PlaceTop(left, cursor);
            cursor += headingHeight + HeadingGap;

            _blocks.SetOrigin(new Vector2(left, cursor));
            cursor += _blocks.Height + SectionGap;
        }

        _sectionHeading.PlaceTop(left, cursor);
        cursor += headingHeight + HeadingGap;

        LayOutSection(left, cursor);

        cursor += SectionHeight + SectionGap;

        _carriedHeading.PlaceTop(left, cursor);
        cursor += headingHeight + HeadingGap;

        _storage.SetOrigin(new Vector2(left, cursor));
        cursor += _storage.Height + HotbarGap;

        _hotbar.SetOrigin(new Vector2(left, cursor));
        cursor += _hotbar.Height + SectionGap;

        float widestHint = _hints.Text.Split('\n').Max(_hints.Measure);
        _hints.PlaceTop(panelLeft + ((panelWidth - widestHint) / 2F), cursor);
    }

    private void WrapHints()
    {
        _hints.SetWrapped(_showsBlockList ? SupplyHints : BenchHints, _showsBlockList ? _blocks.Width : _storage.Width);
    }

    private void LayOutSection(float left, float top)
    {
        switch (_mode)
        {
            case ScreenMode.Chest:
                _chest.SetOrigin(new Vector2(left, top));
                break;

            case ScreenMode.Furnace:
                LayOutFurnace(left + (2F * (_slotSize + _slotGap)), top);
                break;

            default:
                UISlotGrid bench = ActiveBenchGrid;
                bench.SetOrigin(new Vector2(left, top));
                _result.SetOrigin(new Vector2(
                    left + bench.Width + ResultGap,
                    top + ((bench.Height - _slotSize) / 2F)));
                break;
        }
    }

    private void LayOutFurnace(float left, float top)
    {
        _furnaceInput.SetOrigin(new Vector2(left, top));

        float flameTop = top + _slotSize + FlameGap;
        _flameTrack.PixelPositionInCanvas = new Vector2(left + ((_slotSize - FlameWidth) / 2F), flameTop);
        _flameTrack.Dimension = new Vector2(FlameWidth, FlameHeight);

        _furnaceFuel.SetOrigin(new Vector2(left, flameTop + FlameHeight + FlameGap));

        float middle = top + (SectionHeight / 2F);
        float arrowLeft = left + _slotSize + ResultGap;

        _arrowTrack.PixelPositionInCanvas = new Vector2(arrowLeft, middle - (ArrowThickness / 2F));
        _arrowTrack.Dimension = new Vector2(ArrowLength, ArrowThickness);

        _furnaceOutput.SetOrigin(new Vector2(arrowLeft + ArrowLength + ResultGap, middle - (_slotSize / 2F)));
    }
}

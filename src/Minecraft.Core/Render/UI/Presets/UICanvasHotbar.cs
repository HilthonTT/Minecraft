using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// The nine slots along the bottom of the screen, and the name of whatever has just been selected fading
/// above them.
/// <para>
/// It shows the hotbar half of the player's inventory and nothing else — the same nine slots the inventory
/// screen ends with, so a stack dragged down there is under the number keys the moment the screen closes.
/// </para>
/// </summary>
public sealed class UICanvasHotbar : UICanvas
{
    private const float SlotSize = 44F;
    private const float SlotGap = 4F;

    /// <summary>How far the bar sits off the bottom of the screen.</summary>
    private const float BottomMargin = 16F;

    /// <summary>How far the backdrop is drawn out past the slots on every side.</summary>
    private const float BackdropPadding = 5F;

    /// <summary>How far the highlight stands out past the slot it is around.</summary>
    private const float SelectionOutline = 3F;

    private const float NameScale = 0.34F;

    /// <summary>
    /// How many hearts the bar is drawn as. Each one is two of what the server counts, which is what makes a
    /// zombie's three point swing land as a heart and a half.
    /// </summary>
    private const int Hearts = Constants.PLAYER_MAX_HEALTH / 2;

    private const float HeartSize = 9F;
    private const float HeartGap = 3F;

    /// <summary>How far above the bar the hearts sit, and how far above them the block name then goes.</summary>
    private const float HeartMargin = 7F;

    /// <summary>How far above the bar the name of the selected block sits.</summary>
    private const float NameMargin = 12F;

    /// <summary>How long the name of a newly selected block stays up, and how long it then takes to go.</summary>
    private const float NameVisibleSeconds = 2.0F;
    private const float NameFadeSeconds = 0.8F;

    private static readonly Vector3 _backdropColor = new(0.06F, 0.06F, 0.08F);
    private static readonly Vector3 _selectionColor = new(0.92F, 0.92F, 0.95F);
    private static readonly Vector3 _nameColor = new(0.96F, 0.96F, 0.96F);

    // Three shades rather than two, so a heart that is half gone reads as half rather than as gone: the
    // dimmed red is still recognisably a heart with something in it.
    private static readonly Vector3 _fullHeartColor = new(0.86F, 0.16F, 0.18F);
    private static readonly Vector3 _halfHeartColor = new(0.52F, 0.11F, 0.13F);
    private static readonly Vector3 _emptyHeartColor = new(0.14F, 0.09F, 0.10F);

    private const float BackdropTransparency = 0.62F;

    private readonly Game _game;
    private readonly UIImage _backdrop;
    private readonly UIImage _selection;
    private readonly UIImage[] _hearts = new UIImage[Hearts];
    private readonly UIText _name;
    private readonly UISlotGrid _slots;
    private readonly Font _font;

    /// <summary>The counts and the label, drawn after the blocks in the slots rather than behind them.</summary>
    public UIOverlayCanvas Overlay { get; }

    /// <summary>What the label last said, so a reselection of the same block does not restart the fade.</summary>
    private string _shownName = string.Empty;
    private DateTime _nameShownAt = DateTime.Now;

    public override bool IsEnabled
    {
        get => base.IsEnabled;
        set
        {
            base.IsEnabled = value;
            Overlay.IsEnabled = value;
        }
    }

    public UICanvasHotbar(Game game)
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

        // Added in the order they are drawn: the backdrop, then the highlight, which shows only as the border
        // the slot on top of it does not cover, and then the slots themselves.
        _backdrop = new UIImage(this, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = _backdropColor,
            Transparency = BackdropTransparency,
        };
        AddComponentToRender(_backdrop);

        _selection = new UIImage(this, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = _selectionColor,
        };
        AddComponentToRender(_selection);

        _slots = new UISlotGrid(
            this,
            Overlay,
            Inventory.HotbarSlots,
            Inventory.HotbarSlots,
            SlotSize,
            SlotGap);

        // On the panel canvas rather than the overlay: nothing is ever drawn standing in a heart, so there
        // is nothing for it to have to be read over.
        for (int heart = 0; heart < Hearts; heart++)
        {
            _hearts[heart] = new UIImage(this, Vector2.Zero, new Vector2(HeartSize, HeartSize), UITextures.White)
            {
                Color = _emptyHeartColor,
                IsVisible = false,
            };

            AddComponentToRender(_hearts[heart]);
        }

        _name = new UIText(Overlay, _font, Vector2.Zero, new Vector2(NameScale, NameScale), string.Empty)
        {
            Color = _nameColor,
        };
        Overlay.AddComponentToRender(_name);

        LayOut();
    }

    /// <summary>Puts the fading label back to nothing, so the next world does not open on the last one's.</summary>
    public void OnWorldUnloaded()
    {
        _shownName = string.Empty;
        _name.Text = string.Empty;
        _slots.ClearCounts();
    }

    public override void Update()
    {
        Inventory inventory = _game.ClientPlayer.Inventory;

        _selection.PixelPositionInCanvas = _slots.PositionOf(inventory.SelectedHotbarSlot)
                                           - new Vector2(SelectionOutline, SelectionOutline);

        _slots.Refresh(
            _game.MasterRenderer.BlockIcons,
            inventory.GetSlot,
            hoveredIndex: inventory.SelectedHotbarSlot);

        UpdateHearts();
        UpdateSelectedName(inventory);

        // Written to on the line above and on a canvas that may already have been cleaned this frame.
        Overlay.Clean();
    }

    /// <summary>
    /// Draws what the player has left, above the bar. Hidden entirely in creative, where nothing can take
    /// any of it: a bar that is always full says nothing, and takes up the room the block name is read in.
    /// </summary>
    private void UpdateHearts()
    {
        if (_game.ClientPlayer.IsCreative)
        {
            foreach (UIImage heart in _hearts)
            {
                heart.IsVisible = false;
            }

            return;
        }

        int health = _game.ClientPlayer.Health;

        for (int heart = 0; heart < Hearts; heart++)
        {
            int halvesInThisHeart = Math.Clamp(health - (heart * 2), 0, 2);

            _hearts[heart].IsVisible = true;
            _hearts[heart].Color = halvesInThisHeart switch
            {
                2 => _fullHeartColor,
                1 => _halfHeartColor,
                _ => _emptyHeartColor,
            };
        }
    }

    /// <summary>
    /// Names whatever has just been reached for, and fades it out again. What is held is drawn in the
    /// player's hand and shown in its slot, but neither of those says what it is called.
    /// </summary>
    private void UpdateSelectedName(Inventory inventory)
    {
        ItemStack selected = inventory.Selected;
        string name = selected.IsEmpty ? string.Empty : BlockCatalogue.NameOf(selected.Block!);

        if (name != _shownName)
        {
            _shownName = name;
            _nameShownAt = DateTime.Now;
            _name.Text = name;
            CentreName();
        }

        _name.IsVisible = name.Length > 0;
        _name.Transparency = FadeFor((float)(DateTime.Now - _nameShownAt).TotalSeconds);
    }

    private static float FadeFor(float elapsedSeconds)
    {
        if (elapsedSeconds <= NameVisibleSeconds)
        {
            return 1F;
        }

        return Math.Clamp(1F - ((elapsedSeconds - NameVisibleSeconds) / NameFadeSeconds), 0F, 1F);
    }

    protected override void OnDimensionsChanged()
    {
        Overlay.SetDimensions(PixelWidth, PixelHeight);
        LayOut();
    }

    private void LayOut()
    {
        float left = (PixelWidth - _slots.Width) / 2F;
        float top = PixelHeight - BottomMargin - _slots.Height;

        _slots.SetOrigin(new Vector2(left, top));

        _backdrop.PixelPositionInCanvas = new Vector2(left - BackdropPadding, top - BackdropPadding);
        _backdrop.Dimension = new Vector2(
            _slots.Width + (2 * BackdropPadding),
            _slots.Height + (2 * BackdropPadding));

        _selection.Dimension = new Vector2(
            SlotSize + (2 * SelectionOutline),
            SlotSize + (2 * SelectionOutline));

        // The row of hearts sits just above the bar, its left edge lined up with the bar's own, so the two
        // read as one thing rather than as an overlay that happens to be nearby.
        float heartsTop = top - BackdropPadding - HeartMargin - HeartSize;
        for (int heart = 0; heart < Hearts; heart++)
        {
            _hearts[heart].PixelPositionInCanvas = new Vector2(
                left + (heart * (HeartSize + HeartGap)),
                heartsTop);
        }

        CentreName();
    }

    private void CentreName()
    {
        float width = _font.MeasureWidth(_name.Text, NameScale);

        // Sat on the line above the bar by where the ink ends rather than by where the text box does, since
        // glyphs hang below their box by an offset that changes with the font.
        (_, float inkBottom) = _font.MeasureVerticalBounds(_name.Text, NameScale);

        // Above the hearts as well as the bar, so the two never overlap in a world that has both.
        float baseline = PixelHeight - BottomMargin - _slots.Height - BackdropPadding
                         - HeartMargin - HeartSize - NameMargin;

        _name.PixelPositionInCanvas = new Vector2((PixelWidth - width) / 2F, baseline - inkBottom);
    }
}

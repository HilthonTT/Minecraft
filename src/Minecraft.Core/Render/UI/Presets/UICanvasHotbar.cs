using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasHotbar : UICanvas
{
    private const float SlotSize = 44F;
    private const float SlotGap = 4F;

    private const float BottomMargin = 16F;

    private const float BackdropPadding = 5F;

    private const float SelectionOutline = 3F;

    private const float NameScale = 0.34F;

    private const int Hearts = Constants.PLAYER_MAX_HEALTH / 2;

    private const float HeartSize = 9F;
    private const float HeartGap = 3F;

    private const float HeartMargin = 7F;

    private const float NameMargin = 12F;

    private const float NameVisibleSeconds = 2.0F;
    private const float NameFadeSeconds = 0.8F;

    private static readonly Vector3 _backdropColor = new(0.06F, 0.06F, 0.08F);
    private static readonly Vector3 _selectionColor = new(0.92F, 0.92F, 0.95F);
    private static readonly Vector3 _nameColor = new(0.96F, 0.96F, 0.96F);

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

    public UIOverlayCanvas Overlay { get; }

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
            _game.MasterRenderer.ItemIcons,
            inventory.GetSlot,
            hoveredIndex: inventory.SelectedHotbarSlot);

        UpdateHearts();
        UpdateSelectedName(inventory);

        Overlay.Clean();
    }

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

    private void UpdateSelectedName(Inventory inventory)
    {
        ItemStack selected = inventory.Selected;
        string name = selected.IsEmpty ? string.Empty : selected.Item!.Name;

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

        (_, float inkBottom) = _font.MeasureVerticalBounds(_name.Text, NameScale);

        float baseline = PixelHeight - BottomMargin - _slots.Height - BackdropPadding
                         - HeartMargin - HeartSize - NameMargin;

        _name.PixelPositionInCanvas = new Vector2((PixelWidth - width) / 2F, baseline - inkBottom);
    }
}

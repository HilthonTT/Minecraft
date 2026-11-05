using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Textures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UICanvasHotbar : UICanvas
{
    private const float SlotSize = 44F;
    private const float SlotGap = 4F;

    private const float BottomMargin = 14F;
    private const float BackdropPadding = 5F;
    private const float SelectionOutline = 3F;

    private const int Hearts = Constants.PLAYER_MAX_HEALTH / 2;
    private const int Shanks = Constants.PLAYER_MAX_FOOD / 2;

    private const float IconSize = 18F;
    private const float IconGap = 1F;
    private const float IconMargin = 6F;
    private const float NameMargin = 10F;

    private const float NameFadeInSeconds = 0.12F;
    private const float NameVisibleSeconds = 2.0F;
    private const float NameFadeOutSeconds = 0.8F;

    private const float DamageFlashSeconds = 0.35F;
    private const int LowHealth = 6;
    private const int LowFood = 6;

    private static readonly Vector3 _heartColor = new(0.90F, 0.16F, 0.18F);
    private static readonly Vector3 _emptyHeartColor = new(0.30F, 0.12F, 0.13F);
    private static readonly Vector3 _shankColor = new(0.84F, 0.56F, 0.26F);
    private static readonly Vector3 _emptyShankColor = new(0.28F, 0.20F, 0.12F);

    private readonly Game _game;
    private readonly UIPanel _backdrop;
    private readonly UIImage _selection;
    private readonly UIImage _selectionInner;
    private readonly UIImage[] _heartBases = new UIImage[Hearts];
    private readonly UIImage[] _heartFills = new UIImage[Hearts];
    private readonly UIImage[] _shankBases = new UIImage[Shanks];
    private readonly UIImage[] _shankFills = new UIImage[Shanks];
    private readonly UILabel _name;
    private readonly UISlotGrid _slots;

    private string _shownName = string.Empty;
    private double _nameShownAt = double.NegativeInfinity;

    private float _selectionX = float.NaN;
    private int _lastHealth = -1;
    private double _damagedAt = double.NegativeInfinity;

    public UICanvasHotbar(Game game)
        : base(Vector3.Zero, Vector3.Zero, UIScale.CanvasWidth, UIScale.CanvasHeight, RenderSpace.Screen)
    {
        _game = game;

        Overlay = new UIOverlayCanvas(PixelWidth, PixelHeight);

        _backdrop = new UIPanel(this, shadow: false, fill: UITheme.Panel, border: UITheme.Outline, fillTransparency: 0.62F);
        _selection = UIDraw.Rect(this, UITheme.Text);
        _selectionInner = UIDraw.Rect(this, UITheme.Outline);

        _slots = new UISlotGrid(this, Overlay, Inventory.HotbarSlots, Inventory.HotbarSlots, SlotSize, SlotGap);

        for (int heart = 0; heart < Hearts; heart++)
        {
            _heartBases[heart] = UIDraw.Rect(this, _emptyHeartColor, 1F, UITextures.Heart);
            _heartFills[heart] = UIDraw.Rect(this, _heartColor, 1F, UITextures.Heart);
        }

        for (int shank = 0; shank < Shanks; shank++)
        {
            _shankBases[shank] = UIDraw.Rect(this, _emptyShankColor, 1F, UITextures.Shank);
            _shankFills[shank] = UIDraw.Rect(this, _shankColor, 1F, UITextures.Shank);
        }

        _name = new UILabel(Overlay, UITheme.LabelScale, UITheme.Text);

        LayOut();
    }

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

    public void OnWorldUnloaded()
    {
        _shownName = string.Empty;
        _name.Text = string.Empty;
        _lastHealth = -1;
        _slots.ClearCounts();
    }

    public override void Update()
    {
        Inventory inventory = _game.ClientPlayer.Inventory;

        UpdateSelection(inventory.SelectedHotbarSlot);

        _slots.Refresh(_game.MasterRenderer.ItemIcons, inventory.GetSlot, hoveredIndex: inventory.SelectedHotbarSlot);

        bool survival = !_game.ClientPlayer.IsCreative;
        UpdateHearts(survival);
        UpdateShanks(survival);
        UpdateSelectedName(inventory);

        Overlay.Clean();
    }

    protected override void OnDimensionsChanged()
    {
        Overlay.SetDimensions(PixelWidth, PixelHeight);
        LayOut();
    }

    private void UpdateSelection(int selectedSlot)
    {
        float target = _slots.PositionOf(selectedSlot).X;
        _selectionX = float.IsNaN(_selectionX) ? target : UIMotion.Approach(_selectionX, target, 30F);

        float top = _slots.PositionOf(0).Y;
        UIDraw.Place(
            _selection,
            new Vector2(MathF.Round(_selectionX) - SelectionOutline, top - SelectionOutline),
            new Vector2(SlotSize + (2 * SelectionOutline), SlotSize + (2 * SelectionOutline)));
        UIDraw.Place(
            _selectionInner,
            new Vector2(MathF.Round(_selectionX) - 1F, top - 1F),
            new Vector2(SlotSize + 2F, SlotSize + 2F));
    }

    private void UpdateHearts(bool visible)
    {
        int health = _game.ClientPlayer.Health;

        if (_lastHealth >= 0 && health < _lastHealth)
        {
            _damagedAt = UIMotion.NowSeconds;
        }

        _lastHealth = health;

        float flash = 1F - Math.Clamp((float)(UIMotion.NowSeconds - _damagedAt) / DamageFlashSeconds, 0F, 1F);
        float pulse = health <= LowHealth ? 0.8F + (0.2F * MathF.Sin((float)UIMotion.NowSeconds * 10F)) : 1F;
        Vector3 fill = Vector3.Lerp(_heartColor * pulse, Vector3.One, flash * 0.8F);

        for (int heart = 0; heart < Hearts; heart++)
        {
            int halves = Math.Clamp(health - (heart * 2), 0, 2);
            SetIcon(_heartBases[heart], _heartFills[heart], visible, halves, fill, UITextures.Heart, UITextures.HalfHeart);
        }
    }

    private void UpdateShanks(bool visible)
    {
        int food = _game.ClientPlayer.Food;
        float pulse = food <= LowFood ? 0.75F + (0.25F * MathF.Sin((float)UIMotion.NowSeconds * 6F)) : 1F;

        for (int shank = 0; shank < Shanks; shank++)
        {
            int halves = Math.Clamp(food - (shank * 2), 0, 2);
            SetIcon(_shankBases[shank], _shankFills[shank], visible, halves, _shankColor * pulse, UITextures.Shank, UITextures.HalfShank);
        }
    }

    private static void SetIcon(
        UIImage baseImage,
        UIImage fillImage,
        bool visible,
        int halves,
        Vector3 color,
        Texture full,
        Texture half)
    {
        baseImage.IsVisible = visible;
        fillImage.IsVisible = visible && halves > 0;
        fillImage.Color = color;

        if (halves > 0)
        {
            fillImage.Texture = halves == 2 ? full : half;
        }
    }

    private void UpdateSelectedName(Inventory inventory)
    {
        ItemStack selected = inventory.Selected;
        string name = selected.IsEmpty ? string.Empty : selected.Item!.Name;

        if (name != _shownName)
        {
            _shownName = name;
            _nameShownAt = UIMotion.NowSeconds;
            _name.Text = name;
            PlaceName();
        }

        float elapsed = (float)(UIMotion.NowSeconds - _nameShownAt);
        float alpha = UIMotion.Envelope(elapsed, NameFadeInSeconds, NameVisibleSeconds, NameFadeOutSeconds);

        _name.IsVisible = name.Length > 0 && alpha > 0F;
        _name.Transparency = alpha;
    }

    private void LayOut()
    {
        float left = MathF.Round((PixelWidth - _slots.Width) / 2F);
        float top = MathF.Round(PixelHeight - BottomMargin - _slots.Height);

        _slots.SetOrigin(new Vector2(left, top));
        _selectionX = float.NaN;

        _backdrop.SetBounds(
            left - BackdropPadding,
            top - BackdropPadding,
            _slots.Width + (2 * BackdropPadding),
            _slots.Height + (2 * BackdropPadding));

        float iconsTop = top - BackdropPadding - IconMargin - IconSize;
        for (int heart = 0; heart < Hearts; heart++)
        {
            var position = new Vector2(left + (heart * (IconSize + IconGap)), iconsTop);
            UIDraw.Place(_heartBases[heart], position, new Vector2(IconSize, IconSize));
            UIDraw.Place(_heartFills[heart], position, new Vector2(IconSize, IconSize));
        }

        float right = left + _slots.Width;
        for (int shank = 0; shank < Shanks; shank++)
        {
            var position = new Vector2(right - IconSize - (shank * (IconSize + IconGap)), iconsTop);
            UIDraw.Place(_shankBases[shank], position, new Vector2(IconSize, IconSize));
            UIDraw.Place(_shankFills[shank], position, new Vector2(IconSize, IconSize));
        }

        PlaceName();
    }

    private void PlaceName()
    {
        float bottom = PixelHeight - BottomMargin - _slots.Height - BackdropPadding - IconMargin - IconSize - NameMargin;
        _name.PlaceTop(MathF.Round((PixelWidth - _name.Width) / 2F), bottom - _name.CapHeight);
    }
}

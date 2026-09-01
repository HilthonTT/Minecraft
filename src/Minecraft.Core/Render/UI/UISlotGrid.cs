using Minecraft.Core.Inventories;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UISlotGrid
{
    private const float CountScale = 0.19F;

    private const float CountShadowOffset = 1.6F;

    private const float IconFillFraction = 0.72F;

    private const float CountInsetX = 4F;
    private const float CountInsetY = 3F;

    private const float WearBarFraction = 0.68F;
    private const float WearBarThickness = 3F;
    private const float WearBarInset = 5F;

    private static readonly Vector3 _idleColor = new(0.15F, 0.16F, 0.19F);
    private static readonly Vector3 _hoverColor = new(0.34F, 0.38F, 0.45F);
    private static readonly Vector3 _countColor = new(0.97F, 0.97F, 0.97F);
    private static readonly Vector3 _countShadowColor = new(0.04F, 0.04F, 0.05F);
    private static readonly Vector3 _wearTrackColor = new(0.03F, 0.03F, 0.04F);

    private static readonly Vector3 _wearFullColor = new(0.24F, 0.82F, 0.24F);
    private static readonly Vector3 _wearEmptyColor = new(0.88F, 0.16F, 0.12F);

    private readonly UIImage[] _panels;
    private readonly UIText[] _counts;
    private readonly UIText[] _countShadows;

    private readonly UIImage[] _wearTracks;
    private readonly UIImage[] _wearBars;

    private readonly Font _font;

    private Vector2 _origin;

    public int Columns { get; }

    public float SlotSize { get; }

    public float Gap { get; }

    public int Count => _panels.Length;

    public int Rows => (Count + Columns - 1) / Columns;

    public float Width => (Columns * SlotSize) + ((Columns - 1) * Gap);

    public float Height => (Rows * SlotSize) + ((Rows - 1) * Gap);

    public UISlotGrid(
        UICanvas panelCanvas,
        UICanvas overlayCanvas,
        int count,
        int columns,
        float slotSize,
        float gap)
    {
        _font = FontRegistry.GetFont(FontType.Arial);

        Columns = columns;
        SlotSize = slotSize;
        Gap = gap;

        _panels = new UIImage[count];
        _counts = new UIText[count];
        _countShadows = new UIText[count];
        _wearTracks = new UIImage[count];
        _wearBars = new UIImage[count];

        for (int slot = 0; slot < count; slot++)
        {
            _panels[slot] = new UIImage(
                panelCanvas,
                Vector2.Zero,
                new Vector2(slotSize, slotSize),
                UITextures.White)
            {
                Color = _idleColor,
            };
            panelCanvas.AddComponentToRender(_panels[slot]);
        }

        for (int slot = 0; slot < count; slot++)
        {
            _wearTracks[slot] = AddWearBar(overlayCanvas, _wearTrackColor);
        }

        for (int slot = 0; slot < count; slot++)
        {
            _wearBars[slot] = AddWearBar(overlayCanvas, _wearFullColor);
        }

        for (int slot = 0; slot < count; slot++)
        {
            _countShadows[slot] = AddCount(overlayCanvas, _countShadowColor);
        }

        for (int slot = 0; slot < count; slot++)
        {
            _counts[slot] = AddCount(overlayCanvas, _countColor);
        }
    }

    private UIImage AddWearBar(UICanvas overlayCanvas, Vector3 color)
    {
        var bar = new UIImage(overlayCanvas, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = color,
            IsVisible = false,
        };

        overlayCanvas.AddComponentToRender(bar);
        return bar;
    }

    private UIText AddCount(UICanvas overlayCanvas, Vector3 color)
    {
        var text = new UIText(
            overlayCanvas,
            _font,
            Vector2.Zero,
            new Vector2(CountScale, CountScale),
            string.Empty)
        {
            Color = color,
        };

        overlayCanvas.AddComponentToRender(text);
        return text;
    }

    public void SetOrigin(Vector2 topLeft)
    {
        _origin = topLeft;

        for (int slot = 0; slot < Count; slot++)
        {
            _panels[slot].PixelPositionInCanvas = PositionOf(slot);
        }
    }

    public Vector2 PositionOf(int index) => _origin + new Vector2(
        index % Columns * (SlotSize + Gap),
        index / Columns * (SlotSize + Gap));

    public Vector2 CentreOf(int index) => PositionOf(index) + new Vector2(SlotSize / 2F, SlotSize / 2F);

    public int IndexAt(Vector2 point)
    {
        for (int slot = 0; slot < Count; slot++)
        {
            Vector2 position = PositionOf(slot);

            if (point.X >= position.X && point.X <= position.X + SlotSize &&
                point.Y >= position.Y && point.Y <= position.Y + SlotSize)
            {
                return slot;
            }
        }

        return -1;
    }

    public void Refresh(ItemIconRenderer icons, Func<int, ItemStack> stackAt, int hoveredIndex)
    {
        for (int slot = 0; slot < Count; slot++)
        {
            _panels[slot].Color = slot == hoveredIndex ? _hoverColor : _idleColor;

            ItemStack stack = stackAt(slot);
            if (stack.IsEmpty)
            {
                SetCount(slot, string.Empty);
                SetWear(slot, stack);
                continue;
            }

            icons.Queue(stack, CentreOf(slot), SlotSize * IconFillFraction);

            SetCount(slot, stack.Count > 1 ? stack.Count.ToString() : string.Empty);
            SetWear(slot, stack);
        }
    }

    private void SetWear(int slot, ItemStack stack)
    {
        bool worn = !stack.IsEmpty && stack.Item!.IsDamageable && stack.Damage > 0;

        _wearTracks[slot].IsVisible = worn;
        _wearBars[slot].IsVisible = worn;

        if (!worn)
        {
            return;
        }

        float left = stack.RemainingDurability / (float)stack.Item!.MaxDurability;
        float width = SlotSize * WearBarFraction;

        var position = new Vector2(
            PositionOf(slot).X + ((SlotSize - width) / 2F),
            PositionOf(slot).Y + SlotSize - WearBarInset - WearBarThickness);

        _wearTracks[slot].PixelPositionInCanvas = position;
        _wearTracks[slot].Dimension = new Vector2(width, WearBarThickness);

        _wearBars[slot].PixelPositionInCanvas = position;
        _wearBars[slot].Dimension = new Vector2(MathF.Max(1F, width * left), WearBarThickness);
        _wearBars[slot].Color = Vector3.Lerp(_wearEmptyColor, _wearFullColor, left);
    }

    private void SetCount(int slot, string count)
    {
        _counts[slot].Text = count;
        _countShadows[slot].Text = count;

        if (count.Length == 0)
        {
            return;
        }

        (_, float inkBottom) = _font.MeasureVerticalBounds(count, CountScale);
        Vector2 corner = PositionOf(slot) + new Vector2(SlotSize, SlotSize);

        var position = new Vector2(
            corner.X - CountInsetX - _font.MeasureWidth(count, CountScale),
            corner.Y - CountInsetY - inkBottom);

        _counts[slot].PixelPositionInCanvas = position;
        _countShadows[slot].PixelPositionInCanvas = position + new Vector2(CountShadowOffset, CountShadowOffset);
    }

    public void SetVisible(bool isVisible)
    {
        for (int slot = 0; slot < Count; slot++)
        {
            _panels[slot].IsVisible = isVisible;
            _counts[slot].IsVisible = isVisible;
            _countShadows[slot].IsVisible = isVisible;

            if (!isVisible)
            {
                _wearTracks[slot].IsVisible = false;
                _wearBars[slot].IsVisible = false;
            }
        }
    }

    public void ClearCounts()
    {
        for (int slot = 0; slot < Count; slot++)
        {
            SetCount(slot, string.Empty);
            SetWear(slot, ItemStack.Empty);
        }
    }
}

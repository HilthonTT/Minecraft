using System.Globalization;
using Minecraft.Core.Inventories;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UISlotGrid
{
    private const float CountScaleAtReference = 0.21F;
    private const float ReferenceSlotSize = 46F;

    private const float IconFillFraction = 0.72F;

    private const float CountInsetX = 3F;
    private const float CountInsetY = 3F;

    private const float WearBarFraction = 0.72F;
    private const float WearBarThickness = 3F;
    private const float WearBarInset = 4F;

    private const float BevelThickness = 2F;

    private static readonly Vector3 _wearTrackColor = new(0.03F, 0.03F, 0.04F);
    private static readonly Vector3 _wearFullColor = new(0.24F, 0.82F, 0.24F);
    private static readonly Vector3 _wearEmptyColor = new(0.88F, 0.16F, 0.12F);

    private readonly UIImage[] _borders;
    private readonly UIImage[] _panels;
    private readonly UIImage[] _bevels;
    private readonly UILabel[] _counts;

    private readonly UIImage[] _wearTracks;
    private readonly UIImage[] _wearBars;

    private readonly float[] _hover;

    private Vector2 _origin;
    private bool _isVisible = true;

    public UISlotGrid(
        UICanvas panelCanvas,
        UICanvas overlayCanvas,
        int count,
        int columns,
        float slotSize,
        float gap)
    {
        Columns = columns;
        SlotSize = slotSize;
        Gap = gap;

        _borders = new UIImage[count];
        _panels = new UIImage[count];
        _bevels = new UIImage[count];
        _counts = new UILabel[count];
        _wearTracks = new UIImage[count];
        _wearBars = new UIImage[count];
        _hover = new float[count];

        for (int slot = 0; slot < count; slot++)
        {
            _borders[slot] = UIDraw.Rect(panelCanvas, UITheme.SlotBorder, 0.95F);
            _panels[slot] = UIDraw.Rect(panelCanvas, UITheme.Slot, 0.95F);
            _bevels[slot] = UIDraw.Rect(panelCanvas, Vector3.Zero, 0.28F);
        }

        for (int slot = 0; slot < count; slot++)
        {
            _wearTracks[slot] = AddWearBar(overlayCanvas, _wearTrackColor);
            _wearBars[slot] = AddWearBar(overlayCanvas, _wearFullColor);
        }

        float countScale = CountScaleAtReference * slotSize / ReferenceSlotSize;
        for (int slot = 0; slot < count; slot++)
        {
            _counts[slot] = new UILabel(overlayCanvas, countScale, UITheme.Text);
        }
    }

    public int Columns { get; }

    public float SlotSize { get; private set; }

    public float Gap { get; private set; }

    public int Count => _panels.Length;

    public int Rows => (Count + Columns - 1) / Columns;

    public float Width => WidthFor(Columns, SlotSize, Gap);

    public float Height => (Rows * SlotSize) + ((Rows - 1) * Gap);

    public static float WidthFor(int columns, float slotSize, float gap) => (columns * slotSize) + ((columns - 1) * gap);

    public void SetSlotSize(float slotSize, float gap)
    {
        SlotSize = slotSize;
        Gap = gap;
        SetOrigin(_origin);
    }

    public void SetOrigin(Vector2 topLeft)
    {
        _origin = topLeft;

        for (int slot = 0; slot < Count; slot++)
        {
            Vector2 position = PositionOf(slot);
            UIDraw.Place(_borders[slot], position, new Vector2(SlotSize, SlotSize));
            UIDraw.Place(_panels[slot], position + new Vector2(1F, 1F), new Vector2(SlotSize - 2F, SlotSize - 2F));
            UIDraw.Place(_bevels[slot], position + new Vector2(1F, 1F), new Vector2(SlotSize - 2F, BevelThickness));
            _counts[slot].Text = string.Empty;
        }
    }

    public Vector2 PositionOf(int index) => _origin + new Vector2(
        index % Columns * (SlotSize + Gap),
        index / Columns * (SlotSize + Gap));

    public Vector2 CentreOf(int index) => PositionOf(index) + new Vector2(SlotSize / 2F, SlotSize / 2F);

    public int IndexAt(Vector2 point)
    {
        if (!_isVisible)
        {
            return -1;
        }

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
            _hover[slot] = UIMotion.Approach(_hover[slot], slot == hoveredIndex ? 1F : 0F, 24F);
            _panels[slot].Color = Vector3.Lerp(UITheme.Slot, UITheme.SlotHover, _hover[slot]);

            ItemStack stack = stackAt(slot);
            if (stack.IsEmpty)
            {
                SetCount(slot, string.Empty);
                SetWear(slot, stack);
                continue;
            }

            float size = SlotSize * IconFillFraction * (1F + (0.06F * _hover[slot]));
            icons.Queue(stack, UIScale.ToWindow(CentreOf(slot)), UIScale.LengthToWindow(size));

            SetCount(slot, stack.Count > 1 ? stack.Count.ToString(CultureInfo.InvariantCulture) : string.Empty);
            SetWear(slot, stack);
        }
    }

    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;

        for (int slot = 0; slot < Count; slot++)
        {
            _borders[slot].IsVisible = isVisible;
            _panels[slot].IsVisible = isVisible;
            _bevels[slot].IsVisible = isVisible;
            _counts[slot].IsVisible = isVisible;

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

    private static UIImage AddWearBar(UICanvas overlayCanvas, Vector3 color)
    {
        UIImage bar = UIDraw.Rect(overlayCanvas, color);
        bar.IsVisible = false;
        return bar;
    }

    private void SetWear(int slot, ItemStack stack)
    {
        bool worn = _isVisible && !stack.IsEmpty && stack.Item!.IsDamageable && stack.Damage > 0;

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

        UIDraw.Place(_wearTracks[slot], position - new Vector2(1F, 1F), new Vector2(width + 2F, WearBarThickness + 2F));
        UIDraw.Place(_wearBars[slot], position, new Vector2(MathF.Max(1F, width * left), WearBarThickness));
        _wearBars[slot].Color = Vector3.Lerp(_wearEmptyColor, _wearFullColor, left);
    }

    private void SetCount(int slot, string count)
    {
        UILabel label = _counts[slot];
        if (label.Text == count)
        {
            return;
        }

        label.Text = count;
        if (count.Length == 0)
        {
            return;
        }

        float scale = SlotSize / ReferenceSlotSize;
        Vector2 corner = PositionOf(slot) + new Vector2(SlotSize, SlotSize);
        label.PlaceTop(
            corner.X - (CountInsetX * scale) - label.Width - label.ShadowOffset,
            corner.Y - (CountInsetY * scale) - label.CapHeight - label.ShadowOffset);
    }
}

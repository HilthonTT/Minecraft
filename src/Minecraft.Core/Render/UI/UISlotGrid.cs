using Minecraft.Core.Inventories;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

/// <summary>
/// A block of slots laid out in rows: the panel behind each one, whatever is drawn in it, the count in its
/// corner and, for a tool, how much use is left in it. The hotbar, the storage rows, a bench and the list of
/// every block in the game are all one of these, which is what keeps a stack looking and behaving the same
/// wherever it has been put.
/// <para>
/// The panels and the counts live on two different canvases, because the icons are drawn as real geometry in
/// between the two: a panel has to be behind its block and the count has to be in front of it.
/// </para>
/// </summary>
public sealed class UISlotGrid
{
    private const float CountScale = 0.19F;

    /// <summary>
    /// How far the count's shadow is offset behind it. A count is read against whatever block happens to be
    /// under it, and white on sand or on glowstone is nearly nothing without one.
    /// </summary>
    private const float CountShadowOffset = 1.6F;

    /// <summary>How tall the block inside a slot is drawn, as a share of the slot.</summary>
    private const float IconFillFraction = 0.72F;

    /// <summary>Where the count sits in from the bottom right corner of its slot.</summary>
    private const float CountInsetX = 4F;
    private const float CountInsetY = 3F;

    /// <summary>How wide the wear bar runs across its slot, and how thick and how far off the bottom it sits.</summary>
    private const float WearBarFraction = 0.68F;
    private const float WearBarThickness = 3F;
    private const float WearBarInset = 5F;

    private static readonly Vector3 _idleColor = new(0.15F, 0.16F, 0.19F);
    private static readonly Vector3 _hoverColor = new(0.34F, 0.38F, 0.45F);
    private static readonly Vector3 _countColor = new(0.97F, 0.97F, 0.97F);
    private static readonly Vector3 _countShadowColor = new(0.04F, 0.04F, 0.05F);
    private static readonly Vector3 _wearTrackColor = new(0.03F, 0.03F, 0.04F);

    // Green while there is plenty left and red as it runs out, so how worn a tool is can be read at a glance
    // without counting pixels along a bar.
    private static readonly Vector3 _wearFullColor = new(0.24F, 0.82F, 0.24F);
    private static readonly Vector3 _wearEmptyColor = new(0.88F, 0.16F, 0.12F);

    private readonly UIImage[] _panels;
    private readonly UIText[] _counts;
    private readonly UIText[] _countShadows;

    // On the overlay with the counts rather than behind the icons with the panels: a bar drawn under a
    // pickaxe would be hidden by the pickaxe it is about.
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

        // Every track before every bar, since a canvas draws its components in the order it was given them
        // and a bar belongs over its own track, not over the next slot's.
        for (int slot = 0; slot < count; slot++)
        {
            _wearTracks[slot] = AddWearBar(overlayCanvas, _wearTrackColor);
        }

        for (int slot = 0; slot < count; slot++)
        {
            _wearBars[slot] = AddWearBar(overlayCanvas, _wearFullColor);
        }

        // Every shadow before every count, since a canvas draws its components in the order it was given
        // them and a shadow belongs under the number it is behind, not under the next slot's.
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

    /// <summary>Moves the whole block of slots, in canvas pixels, measured from its top left corner.</summary>
    public void SetOrigin(Vector2 topLeft)
    {
        _origin = topLeft;

        for (int slot = 0; slot < Count; slot++)
        {
            _panels[slot].PixelPositionInCanvas = PositionOf(slot);
        }
    }

    /// <summary>The top left corner of one slot.</summary>
    public Vector2 PositionOf(int index) => _origin + new Vector2(
        index % Columns * (SlotSize + Gap),
        index / Columns * (SlotSize + Gap));

    public Vector2 CentreOf(int index) => PositionOf(index) + new Vector2(SlotSize / 2F, SlotSize / 2F);

    /// <summary>Which slot the given point falls in, or -1 for a point in the gaps or outside the block.</summary>
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

    /// <summary>
    /// Brings the slots up to date with what is in them: the highlight under the cursor, the block itself
    /// queued with the icon pass, and the count over its corner. Called once a frame, and cheap when nothing
    /// has moved, since both the text and the panels only rebuild a mesh when a value has really changed.
    /// <para>
    /// The counts written here land on the overlay canvas, which the screen that owns this grid is expected
    /// to clean once it has refreshed all of them.
    /// </para>
    /// </summary>
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

            // A single thing needs no number over it: the icon itself already says there is one.
            SetCount(slot, stack.Count > 1 ? stack.Count.ToString() : string.Empty);
            SetWear(slot, stack);
        }
    }

    /// <summary>
    /// Draws how much use is left in a tool, and nothing at all for anything else or for a tool that has not
    /// been swung yet. A bar under an unused pickaxe would be a full bar under every pickaxe ever made, which
    /// says nothing and only makes the slot noisier.
    /// </summary>
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

    /// <summary>Writes a count into the bottom right corner of its slot, with its shadow behind it.</summary>
    private void SetCount(int slot, string count)
    {
        _counts[slot].Text = count;
        _countShadows[slot].Text = count;

        if (count.Length == 0)
        {
            return;
        }

        // Glyphs hang below the component's own top edge by an offset of their own, so the number is placed
        // by where its ink ends up rather than by where its box starts. Measured against this text and not
        // the tallest the font could be: a count is digits, which have no descender.
        (_, float inkBottom) = _font.MeasureVerticalBounds(count, CountScale);
        Vector2 corner = PositionOf(slot) + new Vector2(SlotSize, SlotSize);

        var position = new Vector2(
            corner.X - CountInsetX - _font.MeasureWidth(count, CountScale),
            corner.Y - CountInsetY - inkBottom);

        _counts[slot].PixelPositionInCanvas = position;
        _countShadows[slot].PixelPositionInCanvas = position + new Vector2(CountShadowOffset, CountShadowOffset);
    }

    /// <summary>
    /// Shows or hides the whole block of slots. Used by the inventory screen, which has a section that only
    /// one of the two game modes has any use for.
    /// </summary>
    public void SetVisible(bool isVisible)
    {
        for (int slot = 0; slot < Count; slot++)
        {
            _panels[slot].IsVisible = isVisible;
            _counts[slot].IsVisible = isVisible;
            _countShadows[slot].IsVisible = isVisible;

            // Only ever hidden here. A bar that should be showing is turned back on by the next refresh,
            // which is what decides whether there is any wear to draw in the first place.
            if (!isVisible)
            {
                _wearTracks[slot].IsVisible = false;
                _wearBars[slot].IsVisible = false;
            }
        }
    }

    /// <summary>Hides every count, for a grid that is being taken off the screen.</summary>
    public void ClearCounts()
    {
        for (int slot = 0; slot < Count; slot++)
        {
            SetCount(slot, string.Empty);
            SetWear(slot, ItemStack.Empty);
        }
    }
}

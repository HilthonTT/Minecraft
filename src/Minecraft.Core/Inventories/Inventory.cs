using Minecraft.Core.Games;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories;

/// <summary>
/// What a player is carrying: nine slots along the bottom of the screen and three rows of storage behind
/// them, addressed as one run so that a slot can be moved from either into the other without either side
/// having to know which is which.
/// <para>
/// In creative it is a drawing board with an endless supply behind it: the hotbar opens filled, placing a
/// block costs nothing, and the block list on the inventory screen hands out whole stacks. In survival it is
/// a container and nothing else — it opens empty, a placement comes out of a stack, and the only way anything
/// gets in is <see cref="TryAdd"/>, which is where a block that has just been broken lands.
/// </para>
/// </summary>
public sealed class Inventory
{
    /// <summary>The row under the number keys.</summary>
    public const int HotbarSlots = 9;

    public const int StorageRows = 3;

    /// <summary>Everything above the hotbar, in rows of the same width so the two line up.</summary>
    public const int StorageSlots = HotbarSlots * StorageRows;

    public const int TotalSlots = HotbarSlots + StorageSlots;

    // Slots 0..8 are the hotbar and 9..35 the storage above it. One array rather than two, so that moving a
    // stack between them is an ordinary write to a slot and not a case to be handled.
    private readonly ItemStack[] _slots = new ItemStack[TotalSlots];

    private int _selectedHotbarSlot;

    /// <summary>Which of the nine is in the player's hand.</summary>
    public int SelectedHotbarSlot
    {
        get => _selectedHotbarSlot;
        private set
        {
            if (_selectedHotbarSlot == value)
            {
                return;
            }

            _selectedHotbarSlot = value;
            OnChangedHandler?.Invoke();
        }
    }

    /// <summary>
    /// The stack on the cursor while the inventory screen is open, which belongs to no slot at all. Held here
    /// rather than on the screen so that closing the screen mid-move cannot lose it.
    /// </summary>
    public ItemStack CursorStack { get; private set; }

    /// <summary>What is in the selected hotbar slot, which is what a right click would build with.</summary>
    public ItemStack Selected => _slots[_selectedHotbarSlot];

    /// <summary>
    /// Raised whenever any slot, the selection or the cursor moves. Watched by the player, which keeps the
    /// block state it places from it, and by the screens that draw the slots.
    /// </summary>
    public event Action? OnChangedHandler;

    /// <summary>
    /// Which mode the player carrying this is in. Held here rather than reached for through the player,
    /// because the screens that draw slots ask it on every frame and the answer decides whether a click on
    /// the block list is a supply or a no-op.
    /// </summary>
    public GameMode GameMode { get; private set; } = GameMode.Creative;

    /// <summary>Whether blocks can be taken out of thin air, which is what creative means for a container.</summary>
    public bool HasEndlessSupply => GameMode == GameMode.Creative;

    public Inventory() => ResetToDefaults();

    public ItemStack GetSlot(int index) => _slots[index];

    public void SetSlot(int index, ItemStack stack)
    {
        _slots[index] = stack;
        OnChangedHandler?.Invoke();
    }

    /// <summary>Whether the given slot is one of the nine rather than one of the rows above them.</summary>
    public static bool IsHotbarSlot(int index) => index is >= 0 and < HotbarSlots;

    /// <summary>
    /// Empties everything and refills the hotbar with what the current mode starts a player carrying. Called
    /// when a world is left, so the next one does not open on what was being carried around the last, and
    /// again the moment the server says which mode the world being joined is played in.
    /// </summary>
    public void ResetToDefaults()
    {
        Array.Clear(_slots);
        CursorStack = ItemStack.Empty;

        // Survival starts with nothing at all. The nine blocks are a drawing board's worth of materials, and
        // handing them over would settle in advance every question the first night is supposed to ask.
        if (HasEndlessSupply)
        {
            for (int slot = 0; slot < BlockPalette.Blocks.Count && slot < HotbarSlots; slot++)
            {
                _slots[slot] = new ItemStack(BlockPalette.Blocks[slot], ItemStack.MaxCount);
            }
        }

        _selectedHotbarSlot = 0;
        OnChangedHandler?.Invoke();
    }

    /// <summary>
    /// Takes the mode the server says this player is in and starts them over on what that mode carries.
    /// <para>
    /// Emptying on the way into survival is the point: a hotbar filled by creative would otherwise be a
    /// hundred blocks of building material that survival never has to earn, and a stack of TNT with it.
    /// </para>
    /// </summary>
    public void ApplyGameMode(GameMode gameMode)
    {
        if (GameMode == gameMode)
        {
            return;
        }

        GameMode = gameMode;
        ResetToDefaults();
    }

    public void SelectHotbarSlot(int slot)
    {
        if (IsHotbarSlot(slot))
        {
            SelectedHotbarSlot = slot;
        }
    }

    /// <summary>Moves the selection along the hotbar, wrapping around either end, which is what the wheel does.</summary>
    public void StepHotbarSelection(int steps)
    {
        SelectedHotbarSlot = ((_selectedHotbarSlot + steps) % HotbarSlots + HotbarSlots) % HotbarSlots;
    }

    /// <summary>
    /// Reaches for the given block, as the middle mouse button does. A hotbar slot already holding it is
    /// selected rather than filled again; otherwise, in creative, it is put into whichever slot is in hand,
    /// so that pointing at something in the world and clicking always ends with it held.
    /// <para>
    /// In survival the second half of that is a way of conjuring a stack out of the ground by looking at it,
    /// so there it reaches no further than the hotbar: it selects a slot that already holds the block, and
    /// otherwise does nothing.
    /// </para>
    /// </summary>
    public void PickBlock(Block block)
    {
        for (int slot = 0; slot < HotbarSlots; slot++)
        {
            if (_slots[slot].Block == block)
            {
                SelectHotbarSlot(slot);
                return;
            }
        }

        if (!HasEndlessSupply)
        {
            return;
        }

        _slots[_selectedHotbarSlot] = new ItemStack(block, ItemStack.MaxCount);
        OnChangedHandler?.Invoke();
    }

    /// <summary>
    /// Takes up to <paramref name="count"/> out of the slot in hand and hands back what came out, which is
    /// what throwing something down does. Unlike <see cref="TryConsumeSelected"/> this really empties the
    /// slot in either mode, because what it takes out has to end up somewhere: it becomes a stack lying on
    /// the ground, and one conjured out of a bottomless creative hand would be a way of printing blocks.
    /// </summary>
    public ItemStack TakeFromSelected(int count)
    {
        ItemStack selected = _slots[_selectedHotbarSlot];
        if (selected.IsEmpty || count <= 0)
        {
            return ItemStack.Empty;
        }

        int taken = Math.Min(count, selected.Count);
        _slots[_selectedHotbarSlot] = selected.WithCount(selected.Count - taken);
        OnChangedHandler?.Invoke();

        return selected.WithCount(taken);
    }

    /// <summary>
    /// Takes one of whatever is in hand, which is what placing a block costs in survival. Reports whether
    /// there was one to take; a creative hand is bottomless and always says yes without spending anything.
    /// </summary>
    public bool TryConsumeSelected()
    {
        if (HasEndlessSupply)
        {
            return true;
        }

        ItemStack selected = _slots[_selectedHotbarSlot];
        if (selected.IsEmpty)
        {
            return false;
        }

        _slots[_selectedHotbarSlot] = selected.WithCount(selected.Count - 1);
        OnChangedHandler?.Invoke();
        return true;
    }

    /// <summary>
    /// Pours a stack into the first slots that will take it, topping up piles of the same block before
    /// opening a new one. Returns whatever would not fit.
    /// <para>
    /// The door everything comes in by: a block walked over on the ground arrives here, and so does whatever
    /// was left on the cursor when the inventory screen was closed.
    /// </para>
    /// </summary>
    public ItemStack TryAdd(ItemStack stack)
    {
        if (stack.IsEmpty)
        {
            return ItemStack.Empty;
        }

        ItemStack remaining = stack;

        for (int slot = 0; slot < TotalSlots && !remaining.IsEmpty; slot++)
        {
            if (!_slots[slot].CanStackWith(remaining))
            {
                continue;
            }

            int moved = Math.Min(_slots[slot].RemainingSpace, remaining.Count);
            _slots[slot] = _slots[slot].WithCount(_slots[slot].Count + moved);
            remaining = remaining.WithCount(remaining.Count - moved);
        }

        for (int slot = 0; slot < TotalSlots && !remaining.IsEmpty; slot++)
        {
            if (!_slots[slot].IsEmpty)
            {
                continue;
            }

            _slots[slot] = remaining;
            remaining = ItemStack.Empty;
        }

        OnChangedHandler?.Invoke();
        return remaining;
    }

    /// <summary>
    /// A click on a slot with the inventory screen open. The left button moves a whole stack and the right
    /// one splits it in half or puts a single block down, which between them is every move worth making
    /// without a modifier key.
    /// </summary>
    public void ClickSlot(int index, bool rightButton)
    {
        ItemStack slot = _slots[index];
        ItemStack cursor = CursorStack;

        if (cursor.IsEmpty)
        {
            TakeFromSlot(index, slot, rightButton);
        }
        else
        {
            PutIntoSlot(index, slot, cursor, rightButton);
        }

        OnChangedHandler?.Invoke();
    }

    private void TakeFromSlot(int index, ItemStack slot, bool rightButton)
    {
        if (slot.IsEmpty)
        {
            return;
        }

        if (!rightButton)
        {
            CursorStack = slot;
            _slots[index] = ItemStack.Empty;
            return;
        }

        // The larger half stays on the cursor, so right clicking a single block picks it up rather than
        // rounding it away to nothing.
        int taken = (slot.Count + 1) / 2;
        CursorStack = slot.WithCount(taken);
        _slots[index] = slot.WithCount(slot.Count - taken);
    }

    private void PutIntoSlot(int index, ItemStack slot, ItemStack cursor, bool rightButton)
    {
        if (rightButton)
        {
            if (!slot.IsEmpty && (!slot.CanStackWith(cursor) || slot.RemainingSpace == 0))
            {
                return;
            }

            _slots[index] = slot.IsEmpty ? cursor.WithCount(1) : slot.WithCount(slot.Count + 1);
            CursorStack = cursor.WithCount(cursor.Count - 1);
            return;
        }

        if (slot.CanStackWith(cursor))
        {
            int moved = Math.Min(slot.RemainingSpace, cursor.Count);
            _slots[index] = slot.WithCount(slot.Count + moved);
            CursorStack = cursor.WithCount(cursor.Count - moved);
            return;
        }

        // Two different blocks, so they trade places: what was in the slot comes up onto the cursor, which
        // is what makes rearranging a full inventory possible without an empty slot to work through.
        _slots[index] = cursor;
        CursorStack = slot;
    }

    /// <summary>
    /// Takes a stack out of thin air onto the cursor, which is what the block list on the screen is. Does
    /// nothing in survival, where there is no thin air to take one out of.
    /// </summary>
    public void TakeFromSupply(Block block, int count)
    {
        if (!HasEndlessSupply)
        {
            return;
        }

        CursorStack = new ItemStack(block, count);
        OnChangedHandler?.Invoke();
    }

    /// <summary>Throws away whatever is on the cursor. Clicking the block list with a full hand does this.</summary>
    public void DiscardCursorStack()
    {
        if (CursorStack.IsEmpty)
        {
            return;
        }

        CursorStack = ItemStack.Empty;
        OnChangedHandler?.Invoke();
    }

    /// <summary>
    /// Puts whatever is on the cursor back into the inventory, called as the screen closes. A stack that will
    /// not fit anywhere is dropped rather than left on a cursor nobody can see any more.
    /// </summary>
    public void ReturnCursorStack()
    {
        if (CursorStack.IsEmpty)
        {
            return;
        }

        ItemStack cursor = CursorStack;
        CursorStack = ItemStack.Empty;
        TryAdd(cursor);
    }
}

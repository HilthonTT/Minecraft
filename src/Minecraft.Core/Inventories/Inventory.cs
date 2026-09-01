using Minecraft.Core.Games;
using Minecraft.Core.Inventories.Crafting;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories;

public sealed class Inventory
{
    public const int HotbarSlots = 9;

    public const int StorageRows = 3;

    public const int StorageSlots = HotbarSlots * StorageRows;

    public const int TotalSlots = HotbarSlots + StorageSlots;

    private readonly ItemStack[] _slots = new ItemStack[TotalSlots];

    private int _selectedHotbarSlot;

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

    public ItemStack CursorStack { get; private set; }

    public CraftingGrid Crafting { get; } = new(2);

    public ItemStack Selected => _slots[_selectedHotbarSlot];

    public event Action? OnChangedHandler;

    public GameMode GameMode { get; private set; } = GameMode.Creative;

    public bool HasEndlessSupply => GameMode == GameMode.Creative;

    public Inventory() => ResetToDefaults();

    public ItemStack GetSlot(int index) => _slots[index];

    public void SetSlot(int index, ItemStack stack)
    {
        _slots[index] = stack;
        OnChangedHandler?.Invoke();
    }

    public static bool IsHotbarSlot(int index) => index is >= 0 and < HotbarSlots;

    public void ResetToDefaults()
    {
        Array.Clear(_slots);
        CursorStack = ItemStack.Empty;
        Crafting.TakeAll();

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

    public void StepHotbarSelection(int steps)
    {
        SelectedHotbarSlot = ((_selectedHotbarSlot + steps) % HotbarSlots + HotbarSlots) % HotbarSlots;
    }

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

    public bool WearSelected()
    {
        if (HasEndlessSupply)
        {
            return false;
        }

        ItemStack selected = _slots[_selectedHotbarSlot];
        if (selected.Tool is null)
        {
            return false;
        }

        ItemStack worn = selected.Worn();
        _slots[_selectedHotbarSlot] = worn;
        OnChangedHandler?.Invoke();

        return worn.IsEmpty;
    }

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

        _slots[index] = cursor;
        CursorStack = slot;
    }

    public void TakeFromSupply(Item item, int count)
    {
        if (!HasEndlessSupply)
        {
            return;
        }

        CursorStack = new ItemStack(item, count);
        OnChangedHandler?.Invoke();
    }

    public void DiscardCursorStack()
    {
        if (CursorStack.IsEmpty)
        {
            return;
        }

        CursorStack = ItemStack.Empty;
        OnChangedHandler?.Invoke();
    }

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

    public void ClickCraftingSlot(CraftingGrid grid, int index, bool rightButton)
    {
        ItemStack slot = grid.GetSlot(index);
        ItemStack cursor = CursorStack;

        if (cursor.IsEmpty)
        {
            if (slot.IsEmpty)
            {
                return;
            }

            if (!rightButton)
            {
                CursorStack = slot;
                grid.SetSlot(index, ItemStack.Empty);
            }
            else
            {
                int taken = (slot.Count + 1) / 2;
                CursorStack = slot.WithCount(taken);
                grid.SetSlot(index, slot.WithCount(slot.Count - taken));
            }
        }
        else if (rightButton)
        {
            if (!slot.IsEmpty && (!slot.CanStackWith(cursor) || slot.RemainingSpace == 0))
            {
                return;
            }

            grid.SetSlot(index, slot.IsEmpty ? cursor.WithCount(1) : slot.WithCount(slot.Count + 1));
            CursorStack = cursor.WithCount(cursor.Count - 1);
        }
        else if (slot.CanStackWith(cursor))
        {
            int moved = Math.Min(slot.RemainingSpace, cursor.Count);
            grid.SetSlot(index, slot.WithCount(slot.Count + moved));
            CursorStack = cursor.WithCount(cursor.Count - moved);
        }
        else
        {
            grid.SetSlot(index, cursor);
            CursorStack = slot;
        }

        OnChangedHandler?.Invoke();
    }

    public void ClickCraftingResult(CraftingGrid grid)
    {
        ItemStack result = grid.Result;
        if (result.IsEmpty)
        {
            return;
        }

        if (CursorStack.IsEmpty)
        {
            CursorStack = result;
        }
        else if (CursorStack.CanStackWith(result) && CursorStack.RemainingSpace >= result.Count)
        {
            CursorStack = CursorStack.WithCount(CursorStack.Count + result.Count);
        }
        else
        {
            return;
        }

        grid.ConsumeOneOfEach();
        OnChangedHandler?.Invoke();
    }

    public void ReturnCraftingGrid(CraftingGrid grid)
    {
        foreach (ItemStack stack in grid.TakeAll())
        {
            TryAdd(stack);
        }
    }
}

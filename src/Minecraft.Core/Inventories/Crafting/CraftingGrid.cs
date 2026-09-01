namespace Minecraft.Core.Inventories.Crafting;

public sealed class CraftingGrid
{
    private readonly ItemStack[] _slots;

    public CraftingGrid(int size)
    {
        Size = size;
        _slots = new ItemStack[size * size];
    }

    public int Size { get; }

    public int SlotCount => _slots.Length;

    public ItemStack Result { get; private set; }

    public ItemStack GetSlot(int index) => _slots[index];

    public void SetSlot(int index, ItemStack stack)
    {
        _slots[index] = stack;
        RefreshResult();
    }

    public bool IsEmpty
    {
        get
        {
            foreach (ItemStack slot in _slots)
            {
                if (!slot.IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void ConsumeOneOfEach()
    {
        for (int slot = 0; slot < _slots.Length; slot++)
        {
            if (!_slots[slot].IsEmpty)
            {
                _slots[slot] = _slots[slot].WithCount(_slots[slot].Count - 1);
            }
        }

        RefreshResult();
    }

    public List<ItemStack> TakeAll()
    {
        List<ItemStack> taken = [];

        for (int slot = 0; slot < _slots.Length; slot++)
        {
            if (_slots[slot].IsEmpty)
            {
                continue;
            }

            taken.Add(_slots[slot]);
            _slots[slot] = ItemStack.Empty;
        }

        RefreshResult();
        return taken;
    }

    public void RefreshResult() => Result = RecipeRegistry.ResultFor(this);
}

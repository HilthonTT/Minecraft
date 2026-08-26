namespace Minecraft.Core.Inventories.Crafting;

/// <summary>
/// The square of slots a recipe is laid out on, and the one slot the result of it appears in.
/// <para>
/// Two of these exist: a two by two carried around in the inventory screen, and a three by three that belongs
/// to whichever bench is open. Both are the same class because a recipe does not care which it is being laid
/// out on, only whether it fits — see <see cref="Recipe.FitsIn"/>.
/// </para>
/// <para>
/// The result slot is worked out from the grid rather than stored: every write to a cell recomputes it. That
/// is a walk of the whole recipe book per click, which at this many recipes is nothing, and it means there is
/// no second copy of the answer to be left stale by an edit that forgot to refresh it.
/// </para>
/// </summary>
public sealed class CraftingGrid
{
    private readonly ItemStack[] _slots;

    public CraftingGrid(int size)
    {
        Size = size;
        _slots = new ItemStack[size * size];
    }

    /// <summary>How many cells across the bench is. The grid is always square.</summary>
    public int Size { get; }

    public int SlotCount => _slots.Length;

    /// <summary>What laying this out would make, or an empty stack when it makes nothing.</summary>
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

    /// <summary>
    /// Takes one of everything laid out, which is what happens when the result is picked up. The recipe is
    /// not consulted: whatever was on the bench made the result, so a cell of it is spent whatever was in it.
    /// </summary>
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

    /// <summary>
    /// Empties the bench and hands back everything that was on it, so that closing a screen mid-recipe does
    /// not quietly eat the ingredients.
    /// </summary>
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

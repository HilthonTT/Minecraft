using Minecraft.Core.Inventories.Items;

namespace Minecraft.Core.Inventories.Crafting;

public sealed class Recipe
{
    public ItemStack Result { get; }

    private readonly Item?[,]? _pattern;

    private readonly Item[]? _ingredients;

    private readonly bool _mirrored;

    public int PatternWidth { get; }

    public int PatternHeight { get; }

    private Recipe(ItemStack result, Item?[,]? pattern, Item[]? ingredients, bool mirrored)
    {
        Result = result;
        _pattern = pattern;
        _ingredients = ingredients;
        _mirrored = mirrored;

        PatternWidth = pattern?.GetLength(1) ?? 0;
        PatternHeight = pattern?.GetLength(0) ?? 0;
    }

    public static Recipe Shaped(ItemStack result, string[] rows, Dictionary<char, Item> key, bool mirrored = true)
    {
        int height = rows.Length;
        int width = rows.Max(row => row.Length);
        var pattern = new Item?[height, width];

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                char symbol = column < rows[row].Length ? rows[row][column] : ' ';
                pattern[row, column] = symbol == ' ' ? null : key[symbol];
            }
        }

        return new Recipe(result, pattern, null, mirrored);
    }

    public static Recipe Shapeless(ItemStack result, params Item[] ingredients) =>
        new(result, null, ingredients, mirrored: false);

    public bool FitsIn(int gridSize) =>
        _pattern is null
            ? _ingredients!.Length <= gridSize * gridSize
            : PatternWidth <= gridSize && PatternHeight <= gridSize;

    public bool Matches(CraftingGrid grid) =>
        _pattern is null ? MatchesShapeless(grid) : MatchesShaped(grid);

    private bool MatchesShapeless(CraftingGrid grid)
    {
        var remaining = new List<Item>(_ingredients!);

        for (int slot = 0; slot < grid.SlotCount; slot++)
        {
            ItemStack stack = grid.GetSlot(slot);
            if (stack.IsEmpty)
            {
                continue;
            }

            if (!remaining.Remove(stack.Item!))
            {
                return false;
            }
        }

        return remaining.Count == 0;
    }

    private bool MatchesShaped(CraftingGrid grid)
    {
        for (int rowOffset = 0; rowOffset + PatternHeight <= grid.Size; rowOffset++)
        {
            for (int columnOffset = 0; columnOffset + PatternWidth <= grid.Size; columnOffset++)
            {
                if (MatchesAt(grid, rowOffset, columnOffset, flipped: false) ||
                    (_mirrored && MatchesAt(grid, rowOffset, columnOffset, flipped: true)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool MatchesAt(CraftingGrid grid, int rowOffset, int columnOffset, bool flipped)
    {
        for (int row = 0; row < grid.Size; row++)
        {
            for (int column = 0; column < grid.Size; column++)
            {
                int patternRow = row - rowOffset;
                int patternColumn = column - columnOffset;

                Item? wanted = null;
                if (patternRow >= 0 && patternRow < PatternHeight &&
                    patternColumn >= 0 && patternColumn < PatternWidth)
                {
                    int readColumn = flipped ? PatternWidth - 1 - patternColumn : patternColumn;
                    wanted = _pattern![patternRow, readColumn];
                }

                if (grid.GetSlot(row * grid.Size + column).Item != wanted)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

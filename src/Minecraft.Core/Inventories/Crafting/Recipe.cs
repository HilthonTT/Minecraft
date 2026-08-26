using Minecraft.Core.Inventories.Items;

namespace Minecraft.Core.Inventories.Crafting;

/// <summary>
/// One thing that can be made, and what has to be laid out to make it.
/// <para>
/// Two shapes of recipe share this class rather than being split into two. A shaped recipe cares where its
/// ingredients sit relative to one another — a pickaxe is a bar across the top and a shaft under the middle
/// of it, and the same three planks in a row with two sticks beside them is nothing at all. A shapeless one
/// cares only what is on the bench: planks are planks wherever the log was put down.
/// </para>
/// </summary>
public sealed class Recipe
{
    /// <summary>What comes out. Always the same stack, however the ingredients happened to be laid out.</summary>
    public ItemStack Result { get; }

    /// <summary>
    /// The pattern, row by row, or null for a shapeless recipe. Null entries are cells that must be empty
    /// once the pattern has been lined up against what is on the bench.
    /// </summary>
    private readonly Item?[,]? _pattern;

    /// <summary>What must be on the bench for a shapeless recipe, in no order, or null for a shaped one.</summary>
    private readonly Item[]? _ingredients;

    /// <summary>
    /// Whether the pattern may be read from the other side. Almost every shaped recipe is symmetric anyway;
    /// the ones that are not are the tools, and a left handed player laying an axe out the other way round
    /// has still laid out an axe.
    /// </summary>
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

    /// <summary>
    /// A recipe read as a picture. Each row is a run of keys, one character per cell, with a space for a cell
    /// that must be left empty; <paramref name="key"/> says what each character stands for.
    /// </summary>
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

    /// <summary>A recipe that is a bag of ingredients rather than a picture of one.</summary>
    public static Recipe Shapeless(ItemStack result, params Item[] ingredients) =>
        new(result, null, ingredients, mirrored: false);

    /// <summary>Whether this recipe fits inside a bench of the given size.</summary>
    public bool FitsIn(int gridSize) =>
        _pattern is null
            ? _ingredients!.Length <= gridSize * gridSize
            : PatternWidth <= gridSize && PatternHeight <= gridSize;

    /// <summary>
    /// Whether what is laid out on the bench makes this. A shaped pattern is slid over every position it
    /// could sit at rather than being required to start in the top left corner, so a recipe laid out in the
    /// middle of a three by three bench is the same recipe laid out in the corner of it.
    /// </summary>
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

            // One slot spends one ingredient however many are piled in it, since crafting takes one from
            // each cell and a stack of eight logs on the bench is still one log's worth of recipe.
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

    /// <summary>
    /// Whether the pattern laid with its top left corner at the given cell accounts for everything on the
    /// bench. Every cell of the bench is checked and not only the ones the pattern covers, so a correct
    /// pattern with a stray plank sitting beside it is not a match.
    /// </summary>
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

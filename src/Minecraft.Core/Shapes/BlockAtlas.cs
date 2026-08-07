using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// The block texture sheet: how it is laid out, and which of its cells are drawn as cut outs.
/// </summary>
/// <remarks>
/// The sheet carries no alpha channel, so the cells that are meant to be see through are drawn against a
/// white background instead. White is a colour real artwork uses as well, though: snow and ice are all but
/// white, and a rule that threw away every white texel would leave them riddled with holes. So the cut out
/// cells are named here, and only those have their white read as nothing.
/// </remarks>
public static class BlockAtlas
{
    public const int SizeInPixels = 256;
    public const int CellSizeInPixels = 16;
    public const int CellsPerRow = SizeInPixels / CellSizeInPixels;

    public static Vector2 Water { get; } = new(13, 12);

    public static Vector2 Rose { get; } = new(12, 0);
    public static Vector2 Dandelion { get; } = new(13, 0);
    public static Vector2 RedMushroom { get; } = new(12, 1);
    public static Vector2 BrownMushroom { get; } = new(13, 1);
    public static Vector2 TallGrass { get; } = new(7, 2);
    public static Vector2 DeadBush { get; } = new(7, 3);
    public static Vector2 SugarCane { get; } = new(9, 4);
    public static Vector2 WheatSeedling { get; } = new(8, 5);
    public static Vector2 WheatGrowing { get; } = new(11, 5);
    public static Vector2 WheatRipe { get; } = new(15, 5);

    /// <summary>
    /// A lit torch. Only the middle two columns of the cell carry the stick and its flame, so the model wears
    /// a slice of this rather than the whole of it.
    /// </summary>
    public static Vector2 Torch { get; } = new(0, 5);

    // A cactus is drawn narrower than its cell, so its artwork carries a blank margin the way a plant does
    // even though it is built as a cube rather than as crossed quads.
    public static Vector2 CactusTop { get; } = new(5, 4);
    public static Vector2 CactusSide { get; } = new(6, 4);
    public static Vector2 CactusBottom { get; } = new(7, 4);

    /// <summary>
    /// Every cell drawn against a white background. Anything not listed here keeps its white, so adding a
    /// plant means adding its cell to this list as well as to the model that wears it.
    /// </summary>
    public static Vector2[] CutOutCells { get; } =
    [
        Rose,
        Dandelion,
        RedMushroom,
        BrownMushroom,
        TallGrass,
        DeadBush,
        SugarCane,
        WheatSeedling,
        WheatGrowing,
        WheatRipe,
        Torch,
        CactusTop,
        CactusSide,
        CactusBottom,
    ];
}

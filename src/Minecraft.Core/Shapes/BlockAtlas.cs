using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

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

    public static Vector2 Torch { get; } = new(0, 5);

    public static Vector2 CactusTop { get; } = new(5, 4);
    public static Vector2 CactusSide { get; } = new(6, 4);
    public static Vector2 CactusBottom { get; } = new(7, 4);

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

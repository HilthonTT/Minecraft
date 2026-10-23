using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public static class BlockAtlas
{
    public const int SizeInPixels = 256;
    public const int CellSizeInPixels = 16;
    public const int CellsPerRow = SizeInPixels / CellSizeInPixels;

    public static Vector2 Water { get; } = new(13, 12);

    public static Vector2 Lava { get; } = new(13, 14);

    public static Vector2 Glass { get; } = new(1, 3);

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

    public static Vector2 OakSapling { get; } = new(15, 0);
    public static Vector2 SpruceSapling { get; } = new(15, 3);
    public static Vector2 BirchSapling { get; } = new(15, 4);

    public static Vector2 Ladder { get; } = new(3, 5);
    public static Vector2 DoorTop { get; } = new(1, 5);
    public static Vector2 DoorBottom { get; } = new(1, 6);

    public static Vector2 FarmlandTop { get; } = new(7, 5);
    public static Vector2 Dirt { get; } = new(2, 0);
    public static Vector2 Planks { get; } = new(4, 0);
    public static Vector2 Cobblestone { get; } = new(0, 1);
    public static Vector2 StoneSlabSide { get; } = new(5, 0);
    public static Vector2 StoneSlabTop { get; } = new(6, 0);
    public static Vector2 SandstoneSide { get; } = new(0, 12);
    public static Vector2 SandstoneTop { get; } = new(0, 11);
    public static Vector2 SandstoneBottom { get; } = new(0, 13);

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
        Glass,
        OakSapling,
        SpruceSapling,
        BirchSapling,
        Ladder,
        DoorTop,
        DoorBottom,
    ];
}

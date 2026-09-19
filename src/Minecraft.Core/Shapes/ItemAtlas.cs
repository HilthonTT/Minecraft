using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public static class ItemAtlas
{
    public const int SizeInPixels = 256;
    public const int CellSizeInPixels = 16;
    public const int CellsPerRow = SizeInPixels / CellSizeInPixels;

    private const int PickaxeRow = 0;
    private const int AxeRow = 1;
    private const int ShovelRow = 2;
    private const int SwordRow = 3;

    public static Vector2 Stick { get; } = new(0, 4);
    public static Vector2 Coal { get; } = new(1, 4);
    public static Vector2 IronIngot { get; } = new(2, 4);
    public static Vector2 GoldIngot { get; } = new(3, 4);
    public static Vector2 Diamond { get; } = new(4, 4);
    public static Vector2 Redstone { get; } = new(5, 4);

    public static Vector2 RawBeef { get; } = new(0, 5);
    public static Vector2 Steak { get; } = new(1, 5);
    public static Vector2 RawPorkchop { get; } = new(2, 5);
    public static Vector2 CookedPorkchop { get; } = new(3, 5);
    public static Vector2 RawMutton { get; } = new(4, 5);
    public static Vector2 CookedMutton { get; } = new(5, 5);
    public static Vector2 RottenFlesh { get; } = new(6, 5);
    public static Vector2 Bread { get; } = new(7, 5);

    public static Vector2 Pickaxe(int materialColumn) => new(materialColumn, PickaxeRow);

    public static Vector2 Axe(int materialColumn) => new(materialColumn, AxeRow);

    public static Vector2 Shovel(int materialColumn) => new(materialColumn, ShovelRow);

    public static Vector2 Sword(int materialColumn) => new(materialColumn, SwordRow);
}

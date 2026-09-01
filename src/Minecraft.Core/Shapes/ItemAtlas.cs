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

    public static Vector2 Pickaxe(int materialColumn) => new(materialColumn, PickaxeRow);

    public static Vector2 Axe(int materialColumn) => new(materialColumn, AxeRow);

    public static Vector2 Shovel(int materialColumn) => new(materialColumn, ShovelRow);

    public static Vector2 Sword(int materialColumn) => new(materialColumn, SwordRow);
}

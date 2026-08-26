using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// The sheet everything that is not a block is drawn from, and where on it each thing sits.
/// <para>
/// Kept apart from <see cref="BlockAtlas"/> rather than squeezed into the spare cells of it, because the two
/// are read in different ways: a block cell is a surface, tiled over a face and lit by which way that face is
/// turned, while an item cell is a silhouette, cut out and shown whole. The block sheet is also nearly full,
/// and a ladder of twenty tools would have gone in wherever there happened to be room.
/// <para>
/// The artwork is generated rather than drawn by hand — see <c>tools/make-item-atlas.py</c>, which lays the
/// same handful of shapes out in the colours of each material. Its output is committed as
/// <c>Resources/items.png</c>, so a build never depends on the script having been run.
/// </para>
/// </summary>
public static class ItemAtlas
{
    public const int SizeInPixels = 256;
    public const int CellSizeInPixels = 16;
    public const int CellsPerRow = SizeInPixels / CellSizeInPixels;

    /// <summary>The row each kind of tool runs along; the column is the material, in the order they ladder.</summary>
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

using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public sealed class BlockModelDirt(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(2, 0));
}

public sealed class BlockModelStone(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(1, 0));
}

public sealed class BlockModelSand(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(2, 1));
}

public sealed class BlockModelOakLeaves(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(5, 3));
}

public sealed class BlockModelGravel(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(3, 1));
}

public sealed class BlockModelPlanks(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(4, 0));
}

public sealed class BlockModelCobblestone(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(0, 1));
}

public sealed class BlockModelBedrock(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(1, 1));
}

public sealed class BlockModelCoalOre(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(2, 2));
}

public sealed class BlockModelIronOre(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(1, 2));
}

public sealed class BlockModelGoldOre(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(0, 2));
}

public sealed class BlockModelRedstoneOre(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(3, 3));
}

public sealed class BlockModelDiamondOre(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(2, 3));
}

public sealed class BlockModelGlowstone(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(9, 2));
}

public sealed class BlockModelMossyCobblestone(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(4, 2));
}

public sealed class BlockModelClay(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(8, 4));
}

public sealed class BlockModelSnow(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(2, 4));
}

public sealed class BlockModelIce(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetUniformUVs(new Vector2(3, 4));
}

/// <summary>Grass under a covering of snow: white on top, and snow spilling over the top of its sides.</summary>
public sealed class BlockModelSnowyGrass(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() =>
        SetUVs(sideCell: new Vector2(4, 4), topCell: new Vector2(2, 4), bottomCell: new Vector2(2, 0));
}

public sealed class BlockModelBirchLog(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() =>
        SetUVs(sideCell: new Vector2(5, 7), topCell: new Vector2(5, 1), bottomCell: new Vector2(5, 1));
}

public sealed class BlockModelSpruceLog(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() =>
        SetUVs(sideCell: new Vector2(4, 7), topCell: new Vector2(5, 1), bottomCell: new Vector2(5, 1));
}

public sealed class BlockModelTnt(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() =>
        SetUVs(sideCell: new Vector2(8, 0), topCell: new Vector2(9, 0), bottomCell: new Vector2(10, 0));
}

public sealed class BlockModelGrass(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() =>
        SetUVs(sideCell: new Vector2(3, 0), topCell: new Vector2(0, 0), bottomCell: new Vector2(2, 0));
}

public sealed class BlockModelSandstone(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() =>
        SetUVs(sideCell: new Vector2(0, 12), topCell: new Vector2(0, 11), bottomCell: new Vector2(0, 13));
}

public sealed class BlockModelOakLog(TextureAtlas textureAtlas) : FullBlockModel(textureAtlas)
{
    protected override void SetStandardUVs() =>
        SetUVs(sideCell: new Vector2(4, 1), topCell: new Vector2(5, 1), bottomCell: new Vector2(5, 1));
}

/// <summary>
/// A cactus is inset from its cell on all four sides, so its faces never line up with a neighbour's and
/// have to be emitted unconditionally.
/// </summary>
public sealed class BlockModelCactus : FullBlockModel
{
    public BlockModelCactus(TextureAtlas textureAtlas) : base(textureAtlas)
    {
        const float near = 0.0625F;
        const float far = 0.9375F;

        _backFace = [new(1, 0, near), new(0, 0, near), new(0, 1, near), new(1, 1, near)];
        _rightFace = [new(far, 0, 1), new(far, 0, 0), new(far, 1, 0), new(far, 1, 1)];
        _frontFace = [new(0, 0, far), new(1, 0, far), new(1, 1, far), new(0, 1, far)];
        _leftFace = [new(near, 0, 0), new(near, 0, 1), new(near, 1, 1), new(near, 1, 0)];

        _back = false;
        _right = false;
        _front = false;
        _left = false;
        _top = false;
        _bottom = false;
        DoubleSidedFaces = true;
    }

    public override BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos)
    {
        return
        [
            new BlockFace(_backFace, _uvBack),
            new BlockFace(_rightFace, _uvRight),
            new BlockFace(_frontFace, _uvFront),
            new BlockFace(_leftFace, _uvLeft),
            new BlockFace(_topFace, _uvTop),
            new BlockFace(_bottomFace, _uvBottom),
        ];
    }

    public override BlockFace[] GetPartialVisibleFaces(BlockState state, Vector3i blockPos, Direction direction)
    {
        return _emptyArray;
    }

    protected override void SetStandardUVs() =>
        SetUVs(
            sideCell: BlockAtlas.CactusSide,
            topCell: BlockAtlas.CactusTop,
            bottomCell: BlockAtlas.CactusBottom);
}

using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public sealed class BlockModelFlower(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetBladeUVs(BlockAtlas.Rose);
}

public sealed class BlockModelDandelion(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetBladeUVs(BlockAtlas.Dandelion);
}

public sealed class BlockModelRedMushroom(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetBladeUVs(BlockAtlas.RedMushroom);
}

public sealed class BlockModelBrownMushroom(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetBladeUVs(BlockAtlas.BrownMushroom);
}

public sealed class BlockModelSugarCane(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetBladeUVs(BlockAtlas.SugarCane);
}

public sealed class BlockModelDeadBush(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetBladeUVs(BlockAtlas.DeadBush);
}

public sealed class BlockModelWheat(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    private Vector2[] _uvHalfMaturity = [];
    private Vector2[] _uvFullMaturity = [];

    protected override void SetStandardUVs()
    {
        SetBladeUVs(BlockAtlas.WheatSeedling);
        _uvHalfMaturity = _textureAtlas.GetTextureCoords(BlockAtlas.WheatGrowing);
        _uvFullMaturity = _textureAtlas.GetTextureCoords(BlockAtlas.WheatRipe);
    }

    public override BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos)
    {
        var wheat = (BlockStateWheat)state;

        Vector2[] uv = wheat.Maturity switch
        {
            1 => _uvHalfMaturity,
            >= 2 => _uvFullMaturity,
            _ => _uvBladeOne,
        };

        return
        [
            new BlockFace(_bladeOneFace, uv),
            new BlockFace(_bladeTwoFace, uv),
        ];
    }
}

public sealed class BlockModelGrassBlade(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    private const float NoiseDetail = 0.75F;

    protected override void SetStandardUVs() => SetBladeUVs(BlockAtlas.TallGrass);

    public override BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos)
    {
        float sample = Noise3DPerlin.Noise(
            blockPos.X * NoiseDetail,
            blockPos.Y * NoiseDetail,
            blockPos.Z * NoiseDetail);

        Vector3 offset = new(sample / 7, 0, sample / 7);
        float scale = MathUtils.ConvertRange(-1, 1, 0.75F, 1, sample);
        Vector3 scaleVector = new(scale, scale, scale);

        Vector3[] bladeOne = new Vector3[_bladeOneFace.Length];
        Vector3[] bladeTwo = new Vector3[_bladeTwoFace.Length];
        for (int i = 0; i < bladeOne.Length; i++)
        {
            bladeOne[i] = _bladeOneFace[i] * scaleVector + offset;
            bladeTwo[i] = _bladeTwoFace[i] * scaleVector + offset;
        }

        return
        [
            new BlockFace(bladeOne, _uvBladeOne),
            new BlockFace(bladeTwo, _uvBladeTwo),
        ];
    }
}

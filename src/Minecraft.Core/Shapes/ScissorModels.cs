using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public sealed class BlockModelFlower(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetBladeUVs(new Vector2(12, 0));
}

public sealed class BlockModelSugarCane(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetBladeUVs(new Vector2(9, 4));
}

public sealed class BlockModelDeadBush(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    protected override void SetStandardUVs() => SetBladeUVs(new Vector2(7, 3));
}

/// <summary>Wheat swaps its texture as the crop matures.</summary>
public sealed class BlockModelWheat(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    private Vector2[] _uvHalfMaturity = [];
    private Vector2[] _uvFullMaturity = [];

    protected override void SetStandardUVs()
    {
        SetBladeUVs(new Vector2(8, 5));
        _uvHalfMaturity = _textureAtlas.GetTextureCoords(new Vector2(11, 5));
        _uvFullMaturity = _textureAtlas.GetTextureCoords(new Vector2(15, 5));
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

/// <summary>
/// Grass blades are jittered in size and position by a noise sample at their block position, so that a
/// field of them does not read as a perfectly regular grid.
/// </summary>
public sealed class BlockModelGrassBlade(TextureAtlas textureAtlas) : ScissorModel(textureAtlas)
{
    private const float NoiseDetail = 0.75F;

    protected override void SetStandardUVs() => SetBladeUVs(new Vector2(7, 2));

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

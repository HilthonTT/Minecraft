using Minecraft.Core.Textures;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public sealed class TorchModel : BlockModel
{
    private const float HalfThickness = 1F / 16F;

    private const float StickHeight = 10F / 16F;

    private const float ArtworkTop = 6F / 16F;

    private const float WallFootOffset = 0.44F;

    private const float WallLean = 0.28F;

    private const float WallRise = 0.22F;

    private readonly BlockFace[]?[] _facesByAttachment = new BlockFace[]?[6];

    public TorchModel(TextureAtlas textureAtlas) : base(textureAtlas)
    {
        BlockFace[] standing = BuildStandingFaces();

        _facesByAttachment[(int)Direction.Bottom] = standing;

        foreach (Direction wall in (Direction[])[Direction.Back, Direction.Right, Direction.Front, Direction.Left])
        {
            _facesByAttachment[(int)wall] = LeanAgainst(standing, DirectionUtil.ToUnit(wall));
        }
    }

    public override BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos)
    {
        Direction attachment = state is BlockStateTorch torch ? torch.Attachment : Direction.Bottom;
        return _facesByAttachment[(int)attachment] ?? _facesByAttachment[(int)Direction.Bottom]!;
    }

    public override BlockFace[] GetPartialVisibleFaces(BlockState state, Vector3i blockPos, Direction direction)
    {
        return _emptyArray;
    }

    private BlockFace[] BuildStandingFaces()
    {
        float min = 0.5F - HalfThickness;
        float max = 0.5F + HalfThickness;
        float top = StickHeight;

        Vector2[] sideUVs = SliceOfCell(ArtworkTop, 1F);

        Vector2[] topUVs = SliceOfCell(ArtworkTop, ArtworkTop + (2F / 16F));
        Vector2[] bottomUVs = SliceOfCell(14F / 16F, 1F);

        return
        [
            new BlockFace([new(max, 0, min), new(min, 0, min), new(min, top, min), new(max, top, min)], sideUVs),
            new BlockFace([new(max, 0, max), new(max, 0, min), new(max, top, min), new(max, top, max)], sideUVs),
            new BlockFace([new(min, 0, max), new(max, 0, max), new(max, top, max), new(min, top, max)], sideUVs),
            new BlockFace([new(min, 0, min), new(min, 0, max), new(min, top, max), new(min, top, min)], sideUVs),
            new BlockFace([new(min, top, min), new(min, top, max), new(max, top, max), new(max, top, min)], topUVs),
            new BlockFace([new(max, 0, min), new(max, 0, max), new(min, 0, max), new(min, 0, min)], bottomUVs),
        ];
    }

    private Vector2[] SliceOfCell(float top, float bottom)
    {
        return _textureAtlas.GetTextureCoords(
            BlockAtlas.Torch,
            new Vector2(7F / 16F, top),
            new Vector2(9F / 16F, bottom));
    }

    private static BlockFace[] LeanAgainst(BlockFace[] standing, Vector3i towardsWall)
    {
        var leaned = new BlockFace[standing.Length];

        for (int i = 0; i < standing.Length; i++)
        {
            Vector3[] positions = standing[i].Positions;
            var moved = new Vector3[positions.Length];

            for (int vertex = 0; vertex < positions.Length; vertex++)
            {
                Vector3 point = positions[vertex];
                float heightUpStick = point.Y / StickHeight;
                float towards = WallFootOffset - (WallLean * heightUpStick);

                moved[vertex] = new Vector3(
                    point.X + (towardsWall.X * towards),
                    point.Y + WallRise,
                    point.Z + (towardsWall.Z * towards));
            }

            leaned[i] = new BlockFace(moved, standing[i].TextureCoords);
        }

        return leaned;
    }
}

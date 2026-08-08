using Minecraft.Core.Textures;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// A torch: a thin upright box wearing the two texel wide slice of its cell that the stick is actually drawn
/// on. It fills no side of its cell, so every face of it is emitted whatever its neighbours are.
/// <para>
/// One shape per way it can be attached, all built once here. A wall torch is the floor torch carried up its
/// wall and sheared so the tip leans out of it, which is the whole of the difference between the two.
/// </para>
/// </summary>
public sealed class TorchModel : BlockModel
{
    /// <summary>Half the thickness of the stick, in blocks. Two texels wide, as the artwork is drawn.</summary>
    private const float HalfThickness = 1F / 16F;

    /// <summary>How tall the stick stands, in blocks, measured from the ground it sits on.</summary>
    private const float StickHeight = 10F / 16F;

    /// <summary>Where the stick starts within its cell vertically, as a fraction of the cell's artwork.</summary>
    private const float ArtworkTop = 6F / 16F;

    /// <summary>How far the foot of a wall torch is pushed into the wall it hangs on.</summary>
    private const float WallFootOffset = 0.44F;

    /// <summary>How much of that offset the tip has given back, which is what makes it lean outwards.</summary>
    private const float WallLean = 0.28F;

    /// <summary>How far up its wall a torch is carried, so it does not appear to grow out of the floor.</summary>
    private const float WallRise = 0.22F;

    /// <summary>
    /// The finished geometry for each way a torch can be attached, indexed by <see cref="Direction"/>. Only
    /// the five a torch can actually take are filled in; a torch never hangs from a ceiling.
    /// </summary>
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

    /// <summary>The five faces of a torch standing on the ground in the middle of its cell.</summary>
    private BlockFace[] BuildStandingFaces()
    {
        float min = 0.5F - HalfThickness;
        float max = 0.5F + HalfThickness;
        float top = StickHeight;

        // The columns the stick occupies, and the rows from its flame down to the bottom of the cell.
        Vector2[] sideUVs = SliceOfCell(ArtworkTop, 1F);

        // The tip is the flame seen from above, and the foot the last two rows of the handle.
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

    /// <summary>
    /// The stick's own two columns of the cell, between the two given rows of it. Both are fractions of the
    /// cell measured from its top left, which is the corner the artwork is laid out from.
    /// </summary>
    private Vector2[] SliceOfCell(float top, float bottom)
    {
        return _textureAtlas.GetTextureCoords(
            BlockAtlas.Torch,
            new Vector2(7F / 16F, top),
            new Vector2(9F / 16F, bottom));
    }

    /// <summary>
    /// Takes a standing torch and hangs it off the wall in the given direction: lifted, pushed back against
    /// the wall at the foot, and sheared so that the further up the stick a point is, the further it has
    /// leaned back out into the room.
    /// </summary>
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

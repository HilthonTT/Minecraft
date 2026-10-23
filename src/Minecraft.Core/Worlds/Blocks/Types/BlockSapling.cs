using Minecraft.Core.Audio;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using Minecraft.Core.Worlds.Decoration.Trees;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockSapling : Block
{
    /// <summary>
    /// How long a sapling takes to grow on average. Each tick is an even chance, so there is no timer to keep in
    /// the save and no moment when a field of saplings all goes off together.
    /// </summary>
    public const float MeanSecondsToGrow = 120F;

    public BlockSapling(ushort id, TreeKind kind) : base(id)
    {
        Kind = kind;
        IsOpaque = false;
        IsTickable = true;
        SoundMaterial = BlockSoundMaterial.Grass;
        SecondsToBreak = 0F;
    }

    public TreeKind Kind { get; }

    public override BlockState GetNewDefaultState() => new BlockStateSimple(this);

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos) => _emptyAABB;

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos)
    {
        const float Inset = 0.15F;

        var min = new Vector3(blockPos.X + Inset, blockPos.Y, blockPos.Z + Inset);
        var max = new Vector3(
            blockPos.X + Constants.CUBE_DIM - Inset,
            blockPos.Y + Constants.CUBE_DIM - Inset,
            blockPos.Z + Constants.CUBE_DIM - Inset);

        return [new AxisAlignedBox(min, max)];
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos) =>
        IsSoil(world.GetBlockAt(blockPos.Down()).GetBlock());

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (world is WorldServer && sourceBlockPos == blockPos.Down() && !IsSoil(world.GetBlockAt(sourceBlockPos).GetBlock()))
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }

    public override void OnTick(BlockState blockState, World world, Vector3i blockPos, float deltaTime)
    {
        if (world is not WorldServer || Random.Shared.NextDouble() >= deltaTime / MeanSecondsToGrow)
        {
            return;
        }

        TryGrow(world, blockPos, Random.Shared);
    }

    /// <summary>
    /// Grows the tree if there is room for its trunk. Leaves only fill air, so a tree grown against a wall is
    /// simply missing the side of its canopy that would have gone through it.
    /// </summary>
    public bool TryGrow(World world, Vector3i blockPos, Random random)
    {
        List<(Vector3i Offset, Block Block)> layout = SaplingTree.Layout(Kind, random);

        Block log = SaplingTree.LogFor(Kind);

        foreach ((Vector3i offset, Block block) in layout)
        {
            if (block == log && offset != Vector3i.Zero && !HasRoomFor(world, blockPos + offset))
            {
                return false;
            }
        }

        world.QueueToRemoveBlockAt(blockPos);

        foreach ((Vector3i offset, Block block) in layout)
        {
            Vector3i target = blockPos + offset;
            bool fits = block == log
                ? offset == Vector3i.Zero || HasRoomFor(world, target)
                : world.IsBlockPositionLoaded(target) && world.GetBlockAt(target).GetBlock() == BlockRegistry.Air;

            if (fits)
            {
                world.QueueToAddBlockAt(target, BlockRegistry.GetState(block));
            }
        }

        return true;
    }

    // Grass and flowers are pushed aside by a trunk the way they are by a placed block; nothing else is.
    private static bool HasRoomFor(World world, Vector3i blockPos)
    {
        if (!world.IsBlockPositionLoaded(blockPos))
        {
            return false;
        }

        Block block = world.GetBlockAt(blockPos).GetBlock();
        return block == BlockRegistry.Air || (block.IsOverridable && !block.IsLiquid);
    }

    private static bool IsSoil(Block block) =>
        block == BlockRegistry.Grass || block == BlockRegistry.Dirt ||
        block == BlockRegistry.SnowyGrass || block == BlockRegistry.Farmland;
}

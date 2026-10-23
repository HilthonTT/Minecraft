using Minecraft.Core.Physics;
using Minecraft.Core.Shapes;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockSlab : Block
{
    public BlockSlab(ushort id, Block fullBlock) : base(id)
    {
        FullBlock = fullBlock;

        IsOpaque = false;
        HasCustomState = true;

        SoundMaterial = fullBlock.SoundMaterial;
        SecondsToBreak = fullBlock.SecondsToBreak;
        HarvestTool = fullBlock.HarvestTool;
        RequiresCorrectTool = fullBlock.RequiresCorrectTool;
        HarvestLevel = fullBlock.HarvestLevel;
    }

    /// <summary>
    /// What two of these make when the second is laid on the first. Merging into the whole block, rather than a
    /// double slab block of its own, keeps the result a plain opaque cube that culls and lights like one.
    /// </summary>
    public Block FullBlock { get; }

    public override BlockState GetNewDefaultState() => new BlockStateSlab(this);

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos) =>
        BlockShapes.InWorld(BlockShapes.Slab(state is BlockStateSlab { IsTop: true }), blockPos);

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos) =>
        GetCollisionBox(state, blockPos);

    /// <summary>
    /// Whether a slab of this kind placed where <paramref name="existing"/> is would fill the other half of it.
    /// </summary>
    public bool CompletesSlab(BlockState existing, BlockState placed) =>
        existing is BlockStateSlab here && here.GetBlock() == this &&
        placed is BlockStateSlab incoming && incoming.GetBlock() == this && incoming.IsTop != here.IsTop;
}

using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockWheat : Block
{
    private const ushort MaxMaturity = 2;
    private const float SecondsToGrow = 30.0F;

    public BlockWheat(ushort id) : base(id)
    {
        IsTickable = true;
        IsOpaque = false;
        HasCustomState = true;
        SoundMaterial = BlockSoundMaterial.Grass;
        SecondsToBreak = 0F;
        IsPlacedByItsOwnItem = false;
    }

    public bool IsRipe(BlockState blockState) => blockState is BlockStateWheat { Maturity: >= MaxMaturity };

    /// <summary>
    /// Ripe wheat gives up the wheat and the seed to plant again; green wheat only the seed that was put in.
    /// </summary>
    public override void AddDrops(BlockState blockState, World world, Vector3i blockPos, Random random, List<ItemStack> drops)
    {
        if (IsRipe(blockState))
        {
            drops.Add(new ItemStack(this, 1));
            drops.Add(new ItemStack(ItemRegistry.Seeds, 1 + random.Next(3)));
            return;
        }

        drops.Add(new ItemStack(ItemRegistry.Seeds, 1));
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateWheat();
    }

    public override void OnTick(BlockState blockState, World world, Vector3i blockPos, float deltaTime)
    {
        if (world is not WorldServer)
        {
            return;
        }

        var wheat = (BlockStateWheat)blockState;
        if (wheat.Maturity >= MaxMaturity)
        {
            return;
        }

        wheat.ElapsedTimeSinceLastGrowth += deltaTime;
        if (wheat.ElapsedTimeSinceLastGrowth < SecondsToGrow)
        {
            return;
        }

        wheat.ElapsedTimeSinceLastGrowth = 0;

        world.QueueToRemoveBlockAt(blockPos);
        var grownWheat = (BlockStateWheat)BlockRegistry.GetState(BlockRegistry.Wheat);
        grownWheat.Maturity = (ushort)(wheat.Maturity + 1);
        world.QueueToAddBlockAt(blockPos, grownWheat);
    }

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        return world.GetBlockAt(blockPos.Down()).GetBlock() == BlockRegistry.Farmland;
    }

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (world is not WorldServer)
        {
            return;
        }

        if (sourceBlockPos == blockPos.Down() && world.GetBlockAt(sourceBlockPos).GetBlock() == BlockRegistry.Air)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }
}

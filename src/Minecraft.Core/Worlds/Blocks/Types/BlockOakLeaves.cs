using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockOakLeaves : Block
{
    private const int SaplingOneIn = 10;

    private const int TrunkSearchRadius = 4;

    public BlockOakLeaves(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Grass;
        SecondsToBreak = 0.2F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateOakLeaves();
    }

    /// <summary>
    /// Leaves still leave themselves, and now and then a sapling as well. Every kind of tree is crowned with the
    /// same leaves, so which sapling it is comes from the nearest trunk.
    /// </summary>
    public override void AddDrops(BlockState blockState, World world, Vector3i blockPos, Random random, List<ItemStack> drops)
    {
        base.AddDrops(blockState, world, blockPos, random, drops);

        if (random.Next(SaplingOneIn) == 0)
        {
            drops.Add(new ItemStack(SaplingFromNearestTrunk(world, blockPos), 1));
        }
    }

    private static Block SaplingFromNearestTrunk(World world, Vector3i blockPos)
    {
        Block sapling = BlockRegistry.OakSapling;
        int nearest = int.MaxValue;

        for (int dx = -TrunkSearchRadius; dx <= TrunkSearchRadius; dx++)
        {
            for (int dy = -TrunkSearchRadius; dy <= TrunkSearchRadius; dy++)
            {
                for (int dz = -TrunkSearchRadius; dz <= TrunkSearchRadius; dz++)
                {
                    int distance = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                    if (distance >= nearest)
                    {
                        continue;
                    }

                    Block? grows = SaplingFor(world.GetBlockAt(blockPos + new Vector3i(dx, dy, dz)).GetBlock());
                    if (grows is not null)
                    {
                        sapling = grows;
                        nearest = distance;
                    }
                }
            }
        }

        return sapling;
    }

    private static Block? SaplingFor(Block log)
    {
        if (log == BlockRegistry.OakLog)
        {
            return BlockRegistry.OakSapling;
        }

        if (log == BlockRegistry.BirchLog)
        {
            return BlockRegistry.BirchSapling;
        }

        return log == BlockRegistry.SpruceLog ? BlockRegistry.SpruceSapling : null;
    }
}

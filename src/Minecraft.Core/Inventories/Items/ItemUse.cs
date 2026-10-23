using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.Types;
using OpenTK.Mathematics;

namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// What a right click does with an item that works on the world without being placed: a hoe tilling the ground,
/// a bucket scooping up or pouring out a fluid. The client asks first, against its own copy of the world, to know
/// whether to send the click and what is left in its hand; the server asks the same question against the real
/// world and is the only one that applies the answer.
/// </summary>
public static class ItemUse
{
    public static bool IsUsable(Item? item) => item is BucketItem or ToolItem { Kind: ToolKind.Hoe };

    /// <summary>An empty bucket is aimed at the fluid it would scoop up; everything else at what is behind it.</summary>
    public static bool AimsAtFluid(Item? item) => item is BucketItem { IsEmpty: true };

    /// <summary>
    /// Uses <paramref name="held"/> on <paramref name="target"/>.
    /// </summary>
    /// <param name="apply">Whether to change the world, which only the server does.</param>
    /// <returns>What the stack in hand becomes, or null when the item does nothing there.</returns>
    public static ItemStack? Use(World world, ItemStack held, Vector3i target, bool apply)
    {
        if (held.IsEmpty || !world.IsBlockPositionLoaded(target))
        {
            return null;
        }

        return held.Item switch
        {
            ToolItem { Kind: ToolKind.Hoe } => Till(world, held, target, apply),
            BucketItem { IsEmpty: true } => Scoop(world, held, target, apply),
            BucketItem { Holds: Block fluid } => Pour(world, fluid, target, apply),
            _ => null,
        };
    }

    private static ItemStack? Till(World world, ItemStack hoe, Vector3i target, bool apply)
    {
        Block ground = world.GetBlockAt(target).GetBlock();
        if (ground != BlockRegistry.Grass && ground != BlockRegistry.Dirt)
        {
            return null;
        }

        Vector3i above = target + Vector3i.UnitY;
        if (!world.IsOutsideBuildHeight(above.Y) && world.GetBlockAt(above).GetBlock() != BlockRegistry.Air)
        {
            return null;
        }

        if (apply)
        {
            world.QueueToRemoveBlockAt(target);
            world.QueueToAddBlockAt(target, BlockRegistry.GetState(BlockRegistry.Farmland));
        }

        return hoe.Worn();
    }

    private static ItemStack? Scoop(World world, ItemStack bucket, Vector3i target, bool apply)
    {
        if (world.GetBlockAt(target).GetBlock() is not BlockFluid { IsSource: true } fluid)
        {
            return null;
        }

        BucketItem full = fluid is BlockLava ? ItemRegistry.LavaBucket : ItemRegistry.WaterBucket;

        if (apply)
        {
            world.QueueToRemoveBlockAt(target);
        }

        return new ItemStack(full, 1);
    }

    private static ItemStack? Pour(World world, Block fluid, Vector3i target, bool apply)
    {
        Block there = world.GetBlockAt(target).GetBlock();

        bool room = there == BlockRegistry.Air ||
                    (there.IsOverridable && !(there is BlockFluid existing && existing.IsSource));
        if (!room)
        {
            return null;
        }

        if (apply)
        {
            if (there != BlockRegistry.Air)
            {
                world.QueueToRemoveBlockAt(target);
            }

            world.QueueToAddBlockAt(target, BlockRegistry.GetState(fluid));
        }

        return new ItemStack(ItemRegistry.Bucket, 1);
    }
}

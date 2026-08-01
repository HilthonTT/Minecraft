using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockTnt : Block
{
    private const int ExplosionRadius = 10;
    private const float FuseSecondsAfterInteraction = 2.0F;
    private const float FuseSecondsAfterChainReaction = 0.2F;

    public BlockTnt(ushort id) : base(id)
    {
        IsTickable = true;
        IsInteractable = true;
        HasCustomState = true;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateTnt();
    }

    public override void OnTick(BlockState blockState, World world, Vector3i blockPos, float deltaTime)
    {
        if (world is not WorldServer)
        {
            return;
        }

        var tnt = (BlockStateTnt)blockState;
        if (tnt.Trigger == ExplosionTrigger.None)
        {
            return;
        }

        tnt.ElapsedSecondsSinceTrigger += deltaTime;

        float fuseSeconds = tnt.Trigger == ExplosionTrigger.PlayerInteraction
            ? FuseSecondsAfterInteraction
            : FuseSecondsAfterChainReaction;

        if (tnt.ElapsedSecondsSinceTrigger > fuseSeconds)
        {
            Explode(tnt, world);
        }
    }

    public override void OnInteract(BlockState blockState, Vector3i blockPos, World world)
    {
        var tnt = (BlockStateTnt)blockState;
        tnt.Trigger = ExplosionTrigger.PlayerInteraction;
        tnt.BlockPosition = blockPos;
    }

    private static void Explode(BlockStateTnt source, World world)
    {
        List<BlockStateTnt> explosives = [];
        List<Vector3i> targets = [];

        for (int x = -ExplosionRadius; x <= ExplosionRadius; x++)
        {
            for (int y = -ExplosionRadius; y <= ExplosionRadius; y++)
            {
                for (int z = -ExplosionRadius; z <= ExplosionRadius; z++)
                {
                    // Carve a sphere rather than the enclosing cube.
                    if (x * x + y * y + z * z > ExplosionRadius * ExplosionRadius)
                    {
                        continue;
                    }

                    Vector3i target = source.BlockPosition + new Vector3i(x, y, z);
                    BlockState state = world.GetBlockAt(target);
                    if (state.GetBlock() == BlockRegistry.Air)
                    {
                        continue;
                    }

                    if (state is BlockStateTnt neighbourTnt && source.BlockPosition != target)
                    {
                        neighbourTnt.BlockPosition = target;
                        explosives.Add(neighbourTnt);
                    }
                    else
                    {
                        targets.Add(target);
                    }
                }
            }
        }

        world.QueueToRemoveBlocksAt(targets);

        // Any TNT caught in the blast lights on a much shorter fuse, which is what produces the chain.
        foreach (BlockStateTnt explosive in explosives)
        {
            explosive.GetBlock().OnInteract(explosive, explosive.BlockPosition, world);
            explosive.Trigger = ExplosionTrigger.Explosive;
        }
    }
}

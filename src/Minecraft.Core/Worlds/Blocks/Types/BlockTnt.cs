using Minecraft.Core.Audio;
using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockTnt : Block
{
    private const int ExplosionRadius = 10;
    private const float FuseSecondsAfterInteraction = 2.0F;
    private const float FuseSecondsAfterChainReaction = 0.2F;

    private const int MaxBlastDamage = 60;

    private const float BlastKnockbackMultiplier = 3.0F;

    public BlockTnt(ushort id) : base(id)
    {
        IsTickable = true;
        IsInteractable = true;
        HasCustomState = true;
        SoundMaterial = BlockSoundMaterial.Grass;
        SecondsToBreak = 0.1F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateTnt();
    }

    public override void OnTick(BlockState blockState, World world, Vector3i blockPos, float deltaTime)
    {
        if (world is not WorldServer serverWorld)
        {
            return;
        }

        var tnt = (BlockStateTnt)blockState;
        if (tnt.Trigger == ExplosionTrigger.None)
        {
            return;
        }

        tnt.BlockPosition = blockPos;
        tnt.ElapsedSecondsSinceTrigger += deltaTime;

        float fuseSeconds = tnt.Trigger == ExplosionTrigger.PlayerInteraction
            ? FuseSecondsAfterInteraction
            : FuseSecondsAfterChainReaction;

        if (tnt.ElapsedSecondsSinceTrigger > fuseSeconds)
        {
            Explode(tnt, serverWorld);
        }
    }

    public override void OnInteract(BlockState blockState, Vector3i blockPos, World world)
    {
        var tnt = (BlockStateTnt)blockState;
        tnt.Trigger = ExplosionTrigger.PlayerInteraction;
        tnt.BlockPosition = blockPos;
    }

    private static void Explode(BlockStateTnt source, WorldServer world)
    {
        List<BlockStateTnt> explosives = [];
        List<Vector3i> targets = [];

        for (int x = -ExplosionRadius; x <= ExplosionRadius; x++)
        {
            for (int y = -ExplosionRadius; y <= ExplosionRadius; y++)
            {
                for (int z = -ExplosionRadius; z <= ExplosionRadius; z++)
                {
                    if (x * x + y * y + z * z > ExplosionRadius * ExplosionRadius)
                    {
                        continue;
                    }

                    Vector3i target = source.BlockPosition + new Vector3i(x, y, z);
                    BlockState state = world.GetBlockAt(target);
                    if (state.GetBlock() == BlockRegistry.Air || !state.GetBlock().IsBreakable)
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

        var centre = new Vector3(
            source.BlockPosition.X + 0.5F,
            source.BlockPosition.Y + 0.5F,
            source.BlockPosition.Z + 0.5F);

        HurtEverythingCaughtInBlast(world, centre);

        world.Game.Server.BroadcastPacket(new ExplosionPacket(centre));

        foreach (BlockStateTnt explosive in explosives)
        {
            explosive.GetBlock().OnInteract(explosive, explosive.BlockPosition, world);
            explosive.Trigger = ExplosionTrigger.Explosive;
        }
    }

    private static void HurtEverythingCaughtInBlast(WorldServer world, Vector3 centre)
    {
        List<Mob> caughtMobs = [];
        List<ServerPlayer> caughtPlayers = [];

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if ((MiddleOf(entity) - centre).Length > ExplosionRadius)
            {
                continue;
            }

            if (entity is Mob mob)
            {
                caughtMobs.Add(mob);
            }
            else if (entity is ServerPlayer player)
            {
                caughtPlayers.Add(player);
            }
        }

        foreach (Mob mob in caughtMobs)
        {
            world.HurtMob(
                mob,
                DamageAt(MiddleOf(mob), centre),
                centre,
                knockbackMultiplier: BlastKnockbackMultiplier * ImpactAt(MiddleOf(mob), centre));
        }

        foreach (ServerPlayer player in caughtPlayers)
        {
            world.HurtPlayer(player, DamageAt(MiddleOf(player), centre));
        }
    }

    private static float ImpactAt(Vector3 middle, Vector3 centre)
    {
        return 1F - ((middle - centre).Length / ExplosionRadius);
    }

    private static int DamageAt(Vector3 middle, Vector3 centre)
    {
        float impact = ImpactAt(middle, centre);
        return (int)(((impact * impact) + impact) / 2F * MaxBlastDamage) + 1;
    }

    private static Vector3 MiddleOf(Entity entity)
    {
        return entity.Position + new Vector3(entity.Width / 2F, entity.Height / 2F, entity.Length / 2F);
    }
}

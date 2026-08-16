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

    /// <summary>
    /// What a mob standing dead centre of the blast takes. Everything in the game has less than a third of
    /// this, so the crater is a crater: what the falloff below decides is not who dies at the middle but how
    /// far out it stops being certain.
    /// </summary>
    private const int MaxBlastDamage = 60;

    /// <summary>
    /// How much harder than a punch a blast throws what it does not kill, at the middle of it. Scaled down
    /// by the same falloff as the damage, so the survivors at the lip are shoved rather than launched.
    /// </summary>
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

        var centre = new Vector3(
            source.BlockPosition.X + 0.5F,
            source.BlockPosition.Y + 0.5F,
            source.BlockPosition.Z + 0.5F);

        HurtEverythingCaughtInBlast(world, centre);

        // Sent as the event itself. What the clients would otherwise get is the hundreds of separate block
        // removals it leaves behind, which arrive one at a time and are indistinguishable from mining.
        world.Game.Server.BroadcastPacket(new ExplosionPacket(centre));

        // Any TNT caught in the blast lights on a much shorter fuse, which is what produces the chain.
        foreach (BlockStateTnt explosive in explosives)
        {
            explosive.GetBlock().OnInteract(explosive, explosive.BlockPosition, world);
            explosive.Trigger = ExplosionTrigger.Explosive;
        }
    }

    /// <summary>
    /// Hurts every mob standing in the blast, hardest at the middle of it and tailing off to nothing at the
    /// edge. The curve is Minecraft's — the square of how far in it is, plus how far in it is, halved — which
    /// is what keeps the middle of a blast lethal to everything while leaving the lip of it survivable.
    /// <para>
    /// It reaches exactly as far as the crater does and no further, unlike the game it is taken from, where
    /// the blast carries past the ground it breaks and is stopped by whatever is in the way. There is no
    /// point measuring what is in the way here: every block inside this sphere is already on its way out of
    /// the world, so a mob could only ever be sheltered by a wall that is about to stop existing.
    /// </para>
    /// </summary>
    private static void HurtEverythingCaughtInBlast(WorldServer world, Vector3 centre)
    {
        // Gathered before a single blow is dealt. Killing a mob takes it out of the collection, and a player
        // who dies is moved to the spawn, and that is the collection being walked.
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

        // Nobody is thrown by it. A player's body is simulated on their own machine and only reported here,
        // so there is nothing on this side to add a velocity to; what a blast does to somebody standing in
        // one is take the bar off them, which is the half of it that is this side's to decide.
        foreach (ServerPlayer player in caughtPlayers)
        {
            world.HurtPlayer(player, DamageAt(MiddleOf(player), centre));
        }
    }

    /// <summary>How far into the blast a body is: one at the centre of it, nothing at the edge.</summary>
    private static float ImpactAt(Vector3 middle, Vector3 centre)
    {
        return 1F - ((middle - centre).Length / ExplosionRadius);
    }

    /// <summary>What the blast is worth at that distance, falling away towards the lip of the crater.</summary>
    private static int DamageAt(Vector3 middle, Vector3 centre)
    {
        float impact = ImpactAt(middle, centre);
        return (int)(((impact * impact) + impact) / 2F * MaxBlastDamage) + 1;
    }

    /// <summary>
    /// The middle of a body, which is what the blast is measured to. An entity's position is where its feet
    /// are, and measuring to those would have a blast going off at head height read as further away from a
    /// tall mob than from a short one standing beside it.
    /// </summary>
    private static Vector3 MiddleOf(Entity entity)
    {
        return entity.Position + new Vector3(entity.Width / 2F, entity.Height / 2F, entity.Length / 2F);
    }
}

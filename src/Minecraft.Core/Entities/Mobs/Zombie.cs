using Minecraft.Core.Entities.Player;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>
/// A hostile mob that walks at the nearest player within reach, and hits them once it gets there.
/// <para>
/// Being hit does not put it off, which is the whole difference between this and an animal: where a sheep
/// bolts, a zombie takes note of who did it and keeps after that one in particular, further out than it
/// would have noticed anybody in the first place.
/// </para>
/// </summary>
public sealed class Zombie : Mob
{
    public const float BodyWidth = 0.6F;
    public const float BodyHeight = 1.8F;
    public const float BodyLength = 0.6F;

    /// <summary>Twice what any of the animals has, and the most of anything in the world.</summary>
    public const int FullHealth = 20;

    /// <summary>How far away a zombie notices a player.</summary>
    private const float AggroRadius = 24F;

    /// <summary>
    /// How long it keeps after whoever hit it, in ticks. Long enough to outlast a fight and to follow
    /// somebody who has backed off out of sight, since walking away is not meant to be enough to end one.
    /// </summary>
    private const int TicksHuntingAttacker = 200;

    private const int WanderRadius = 6;
    private const int TicksBetweenDecisions = 30;
    private const int OneInChanceOfMoving = 2;

    /// <summary>
    /// How close it has to be to land a blow. A little over an arm's length from the middle of one body to
    /// the middle of the other, so a zombie walking into somebody connects rather than standing on their toes
    /// looking for another half block.
    /// </summary>
    private const float AttackReach = 1.5F;

    /// <summary>
    /// What one blow takes off, and how long it then waits. Minecraft's own figure for normal difficulty,
    /// against the twenty a player carries: seven blows to kill somebody standing still, which is long enough
    /// to notice and run and short enough that being cornered by three of them is the end of it.
    /// </summary>
    private const int AttackDamage = 3;
    private const int TicksBetweenAttacks = 20;

    private int _attackerId = -1;
    private int _huntingTicksRemaining;
    private int _ticksUntilNextAttack;

    public Zombie(int id, World? world, Vector3 position)
        : base(id, world, position, EntityType.Zombie, FullHealth)
    {
    }

    public override bool IsHostile => true;

    protected override float MoveSpeed => 26F;

    protected override void SetInitialDimensions()
    {
        _width = BodyWidth;
        _height = BodyHeight;
        _length = BodyLength;
    }

    protected override void OnHurtBy(Vector3 from, Entity? attacker)
    {
        // A blast has nobody behind it to bear a grudge against, so one leaves the zombie on whatever it was
        // already doing rather than sending it off towards where the stick happened to be standing.
        if (attacker is null)
        {
            return;
        }

        _attackerId = attacker.ID;
        _huntingTicksRemaining = TicksHuntingAttacker;
    }

    protected override void DecideWhatToDo(WorldServer world)
    {
        TryAttackSomebodyWithinReach(world);

        if (TryHuntAttacker(world))
        {
            return;
        }

        // Re-aimed every tick, so the zombie follows a player who is moving rather than heading for where
        // they were standing when it first noticed them.
        ServerPlayer? player = FindNearestPlayer(world, Position, AggroRadius);
        if (player is not null)
        {
            SetTarget(player.Position);
            return;
        }

        // Whatever it was chasing has gone. It finishes walking to where they were last seen, then wanders.
        TickWandering(WanderRadius, TicksBetweenDecisions, OneInChanceOfMoving);
    }

    /// <summary>
    /// Swings at whoever it has caught up with. Aimed at the nearest player within arm's length rather than
    /// at whoever it was walking towards, since a zombie that has been jostled off course by a crowd is
    /// still standing next to somebody.
    /// <para>
    /// The blow itself is the world's to deal, which is what keeps a creative player untouchable and a dead
    /// one back at the spawn without the zombie knowing about either.
    /// </para>
    /// </summary>
    private void TryAttackSomebodyWithinReach(WorldServer world)
    {
        if (_ticksUntilNextAttack > 0)
        {
            _ticksUntilNextAttack--;
            return;
        }

        // Measured from the middle of the body rather than the feet, so standing on a step does not put
        // somebody out of reach of a zombie pressed against them.
        Vector3 chest = Position + new Vector3(0F, BodyHeight / 2F, 0F);
        ServerPlayer? victim = FindNearestPlayer(world, chest, AttackReach);
        if (victim is null)
        {
            return;
        }

        _ticksUntilNextAttack = TicksBetweenAttacks;
        world.HurtPlayer(victim, AttackDamage);
    }

    /// <summary>
    /// Sends the zombie after whoever last hit it, for as long as that lasts. False once the grudge has run
    /// out or the player it was against has left, which puts it back on whoever is nearest.
    /// </summary>
    private bool TryHuntAttacker(WorldServer world)
    {
        if (_huntingTicksRemaining <= 0)
        {
            return false;
        }

        _huntingTicksRemaining--;

        if (!world.LoadedEntities.TryGetValue(_attackerId, out Entity? attacker) || attacker is not ServerPlayer)
        {
            _huntingTicksRemaining = 0;
            return false;
        }

        SetTarget(attacker.Position);
        return true;
    }
}

using Minecraft.Core.Entities.Player;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>
/// A hostile mob that walks at the nearest player within reach, and wanders when there is nobody to follow.
/// It cannot hurt anyone yet; reaching the player is as far as it goes.
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

    private int _attackerId = -1;
    private int _huntingTicksRemaining;

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

    protected override void OnHurtBy(Entity attacker)
    {
        _attackerId = attacker.ID;
        _huntingTicksRemaining = TicksHuntingAttacker;
    }

    protected override void DecideWhatToDo(WorldServer world)
    {
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

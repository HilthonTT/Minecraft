using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>
/// A passive mob that grazes: it ambles to a nearby spot, stands about for a while, then picks another. Every
/// animal in the game does exactly that, so the behaviour lives here and each kind only says how far it will
/// go, how briskly, and how often it can be bothered to.
/// <para>
/// The one thing that interrupts it is being hit, which sends the animal bolting for a few seconds. It has
/// nothing to fight back with, so running is the whole of what a passive mob does about being attacked.
/// </para>
/// </summary>
public abstract class Animal : Mob
{
    /// <summary>How long an animal keeps running after a blow. Extended by every further blow it takes.</summary>
    private const int PanicTicks = 60;

    /// <summary>How much faster than its amble a frightened animal moves.</summary>
    private const float PanicSpeedMultiplier = 2.0F;

    /// <summary>How far ahead a bolting animal aims, re-picked every time it gets there.</summary>
    private const int FleeDistance = 10;

    /// <summary>
    /// How far either side of straight away a flight is aimed. Without it an animal runs down the exact line
    /// between it and whoever hit it, which is easy to follow and reads as a rail rather than as panic.
    /// </summary>
    private const float FleeVeerRadians = MathF.PI / 4F;

    private Vector3 _fleeingFrom;
    private int _panicTicksRemaining;

    protected Animal(int id, World? world, Vector3 position, EntityType entityType, int maxHealth)
        : base(id, world, position, entityType, maxHealth)
    {
    }

    public sealed override bool IsHostile => false;

    /// <summary>How far away the animal will pick its next spot.</summary>
    protected abstract int WanderRadius { get; }

    /// <summary>Ticks between two decisions about whether to move on.</summary>
    protected abstract int TicksBetweenDecisions { get; }

    /// <summary>
    /// One decision in this many sends the animal somewhere; the rest leave it where it is. Kept well above
    /// one so a herd that appeared together does not then move as one body.
    /// </summary>
    protected abstract int OneInChanceOfMoving { get; }

    protected sealed override float CurrentMoveSpeed =>
        _panicTicksRemaining > 0 ? MoveSpeed * PanicSpeedMultiplier : MoveSpeed;

    protected sealed override void OnHurtBy(Entity attacker)
    {
        _fleeingFrom = attacker.Position;
        _panicTicksRemaining = PanicTicks;

        // Aimed here rather than left to the next tick, so the animal is already moving on the frame it is
        // knocked back rather than standing in the blow for a twentieth of a second first.
        RunFromWhatHitIt();
    }

    protected sealed override void DecideWhatToDo(WorldServer world)
    {
        if (_panicTicksRemaining > 0)
        {
            _panicTicksRemaining--;

            // Ten blocks is further than most of these get in the time they have, so this mostly matters to
            // one that ran into a wall and stopped: it picks a fresh line rather than standing against it.
            if (!HasTarget)
            {
                RunFromWhatHitIt();
            }

            return;
        }

        TickWandering(WanderRadius, TicksBetweenDecisions, OneInChanceOfMoving);
    }

    private void RunFromWhatHitIt()
    {
        var away = new Vector3(Position.X - _fleeingFrom.X, 0, Position.Z - _fleeingFrom.Z);
        away = away.LengthSquared < 0.0001F ? _moveForward : away.Normalized();

        float veer = (Random.Shared.NextSingle() - 0.5F) * FleeVeerRadians;
        float sin = MathF.Sin(veer);
        float cos = MathF.Cos(veer);
        away = new Vector3((away.X * cos) - (away.Z * sin), 0, (away.X * sin) + (away.Z * cos));

        SetTarget(Position + (away * FleeDistance));
    }
}

using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

public abstract class Animal : Mob
{
    private const int PanicTicks = 60;

    private const float PanicSpeedMultiplier = 2.0F;

    private const int FleeDistance = 10;

    private const float FleeVeerRadians = MathF.PI / 4F;

    private Vector3 _fleeingFrom;
    private int _panicTicksRemaining;

    protected Animal(int id, World? world, Vector3 position, EntityType entityType, int maxHealth)
        : base(id, world, position, entityType, maxHealth)
    {
    }

    public sealed override bool IsHostile => false;

    protected abstract int WanderRadius { get; }

    protected abstract int TicksBetweenDecisions { get; }

    protected abstract int OneInChanceOfMoving { get; }

    protected sealed override float CurrentMoveSpeed =>
        _panicTicksRemaining > 0 ? MoveSpeed * PanicSpeedMultiplier : MoveSpeed;

    protected sealed override void OnHurtBy(Vector3 from, Entity? attacker)
    {
        _fleeingFrom = from;
        _panicTicksRemaining = PanicTicks;

        RunFromWhatHitIt();
    }

    protected sealed override void DecideWhatToDo(WorldServer world)
    {
        if (_panicTicksRemaining > 0)
        {
            _panicTicksRemaining--;

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

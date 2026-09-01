using Minecraft.Core.Entities.Player;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

public abstract class Mob : Entity
{
    private const float ArrivalDistance = 0.6F;

    private const float JumpForce = Constants.PLAYER_JUMP_FORCE;

    private const float HurtSeconds = 0.5F;

    private const float KnockbackSpeed = 7F;

    private const float KnockbackLift = JumpForce * 0.45F;

    private Vector3 _target;
    private bool _shouldJump;
    private int _ticksUntilNextWanderDecision;
    private float _hurtSecondsRemaining;

    private int _lastDamageTaken;

    protected bool HasTarget { get; private set; }

    public int Health { get; private set; }

    public bool IsAlive => Health > 0;

    public bool IsHurt => _hurtSecondsRemaining > 0F;

    public abstract bool IsHostile { get; }

    protected abstract float MoveSpeed { get; }

    protected virtual float CurrentMoveSpeed => MoveSpeed;

    protected Mob(int id, World? world, Vector3 position, EntityType entityType, int maxHealth)
        : base(id, world, position, entityType)
    {
        Health = maxHealth;
    }

    public bool TryHurt(int damage, Vector3 from, Entity? attacker = null, float knockbackMultiplier = 1F)
    {
        if (!IsAlive || (IsHurt && damage <= _lastDamageTaken))
        {
            return false;
        }

        int landed = IsHurt ? damage - _lastDamageTaken : damage;
        _lastDamageTaken = damage;

        Health = Math.Max(Health - landed, 0);
        ShowHurt();
        ThrowBackwardsAwayFrom(from, knockbackMultiplier);
        OnHurtBy(from, attacker);
        return true;
    }

    public void ShowHurt() => _hurtSecondsRemaining = HurtSeconds;

    protected virtual void OnHurtBy(Vector3 from, Entity? attacker)
    {
    }

    private void ThrowBackwardsAwayFrom(Vector3 source, float multiplier)
    {
        var away = new Vector3(Position.X - source.X, 0, Position.Z - source.Z);

        away = away.LengthSquared < 0.0001F ? _moveForward : away.Normalized();

        Velocity.X = away.X * KnockbackSpeed * multiplier;
        Velocity.Z = away.Z * KnockbackSpeed * multiplier;

        if (!_isInAir)
        {
            _verticalSpeed = KnockbackLift * multiplier;
            _isInAir = true;
        }
    }

    public override void Update(float deltaTime, World world)
    {
        _hurtSecondsRemaining = MathF.Max(_hurtSecondsRemaining - deltaTime, 0F);

        if (world is not WorldServer)
        {
            InterpolateTowardsServerState(deltaTime);
            base.Update(deltaTime, world);
            return;
        }

        Acceleration = Vector3.Zero;

        if (HasTarget)
        {
            WalkTowardsTarget();
        }

        TryJumpIfAsked();

        ApplyVelocityAndCheckCollision(deltaTime, world);
        base.Update(deltaTime, world);
    }

    public override void Tick(float deltaTime, World world)
    {
        base.Tick(deltaTime, world);

        if (world is WorldServer serverWorld)
        {
            DecideWhatToDo(serverWorld);
        }
    }

    protected abstract void DecideWhatToDo(WorldServer world);

    protected void SetTarget(Vector3 target)
    {
        _target = target;
        HasTarget = true;
    }

    private void WalkTowardsTarget()
    {
        Vector3 toTarget = _target - Position;
        toTarget.Y = 0;

        if (toTarget.LengthSquared <= ArrivalDistance * ArrivalDistance)
        {
            HasTarget = false;
            return;
        }

        Yaw = MathF.Atan2(toTarget.X, toTarget.Z);
        UpdateMovementBasisFromYaw();
        MoveHorizontally(0, CurrentMoveSpeed);
    }

    protected override void OnHorizontalCollision()
    {
        _shouldJump = true;
    }

    private void TryJumpIfAsked()
    {
        if (!_shouldJump)
        {
            return;
        }

        _shouldJump = false;

        if (_isInAir)
        {
            return;
        }

        _verticalSpeed = JumpForce;
        _isInAir = true;
    }

    protected void TickWandering(int radius, int ticksBetweenDecisions, int oneInChanceOfMoving)
    {
        if (HasTarget)
        {
            return;
        }

        if (_ticksUntilNextWanderDecision > 0)
        {
            _ticksUntilNextWanderDecision--;
            return;
        }

        _ticksUntilNextWanderDecision = ticksBetweenDecisions;

        if (Random.Shared.Next(oneInChanceOfMoving) != 0)
        {
            return;
        }

        SetTarget(Position + new Vector3(
            Random.Shared.Next(-radius, radius + 1),
            0,
            Random.Shared.Next(-radius, radius + 1)));
    }

    protected static ServerPlayer? FindNearestPlayer(World world, Vector3 from, float maxDistance)
    {
        ServerPlayer? nearest = null;
        float nearestDistanceSquared = maxDistance * maxDistance;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is not ServerPlayer player)
            {
                continue;
            }

            float distanceSquared = (player.Position - from).LengthSquared;
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearest = player;
            }
        }

        return nearest;
    }
}

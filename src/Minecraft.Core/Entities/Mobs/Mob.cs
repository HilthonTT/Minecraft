using Minecraft.Core.Entities.Player;
using Minecraft.Core.Inventories;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.Types;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

public abstract class Mob : Entity
{
    private const float ArrivalDistance = 0.6F;

    private const float JumpForce = Constants.PLAYER_JUMP_FORCE;

    private const float HurtSeconds = 0.5F;

    private const float KnockbackSpeed = 7F;

    private const float KnockbackLift = JumpForce * 0.45F;

    private const int TicksWithoutProgressBeforeGivingUp = 40;

    private const int MaxTicksTowardsTarget = 400;

    private const float MinProgressPerCheck = 0.25F;

    private const int MaxSafeDrop = 3;

    private const float StepLookAhead = 0.8F;

    private const float GroundProbeEpsilon = 0.01F;

    private Vector3 _target;
    private float _closestDistanceToTarget;
    private int _ticksWithoutProgress;
    private int _ticksTowardsTarget;
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

    public virtual IEnumerable<ItemStack> RollDrops(Random random) => [];

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
            WalkTowardsTarget(world);
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
            TrackProgressTowardsTarget();
            DecideWhatToDo(serverWorld);
        }
    }

    private void TrackProgressTowardsTarget()
    {
        if (!HasTarget)
        {
            return;
        }

        _ticksTowardsTarget++;

        float distance = HorizontalDistanceToTarget();
        if (distance < _closestDistanceToTarget - MinProgressPerCheck)
        {
            _closestDistanceToTarget = distance;
            _ticksWithoutProgress = 0;
        }
        else
        {
            _ticksWithoutProgress++;
        }

        if (_ticksWithoutProgress >= TicksWithoutProgressBeforeGivingUp || _ticksTowardsTarget >= MaxTicksTowardsTarget)
        {
            HasTarget = false;
        }
    }

    private float HorizontalDistanceToTarget()
    {
        Vector3 toTarget = _target - Position;
        toTarget.Y = 0;
        return toTarget.Length;
    }

    protected abstract void DecideWhatToDo(WorldServer world);

    protected void SetTarget(Vector3 target)
    {
        bool isNewTarget = !HasTarget || (target - _target).LengthSquared > 1F;

        _target = target;
        HasTarget = true;

        if (isNewTarget)
        {
            _closestDistanceToTarget = HorizontalDistanceToTarget();
            _ticksWithoutProgress = 0;
            _ticksTowardsTarget = 0;
        }
    }

    private void WalkTowardsTarget(World world)
    {
        Vector3 toTarget = _target - Position;
        toTarget.Y = 0;

        if (toTarget.LengthSquared <= ArrivalDistance * ArrivalDistance)
        {
            HasTarget = false;
            return;
        }

        Vector3 direction = toTarget.Normalized();
        Vector3 center = Position + new Vector3(_width / 2F, 0F, _length / 2F);
        if (!IsSafeToStandAt(world, center + (direction * StepLookAhead), avoidWater: false))
        {
            HasTarget = false;
            return;
        }

        Yaw = MathF.Atan2(toTarget.X, toTarget.Z);
        UpdateMovementBasisFromYaw();
        MoveHorizontally(0, CurrentMoveSpeed);
    }

    private static bool IsSafeToStandAt(World world, Vector3 position, bool avoidWater)
    {
        int x = (int)MathF.Floor(position.X);
        int z = (int)MathF.Floor(position.Z);
        int feetY = (int)MathF.Floor(position.Y + GroundProbeEpsilon);

        for (int y = feetY; y >= feetY - 1 - MaxSafeDrop; y--)
        {
            var blockPos = new Vector3i(x, y, z);
            BlockState state = world.GetBlockAt(blockPos);
            Block block = state.GetBlock();

            if (block is BlockLava || (avoidWater && block is BlockFluid))
            {
                return false;
            }

            if (block is not BlockFluid && block.GetCollisionBox(state, blockPos).Length > 0)
            {
                return true;
            }
        }

        return false;
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

    protected void TickWandering(World world, int radius, int ticksBetweenDecisions, int oneInChanceOfMoving)
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

        Vector3 wanderTarget = Position + new Vector3(
            Random.Shared.Next(-radius, radius + 1),
            0,
            Random.Shared.Next(-radius, radius + 1));

        Vector3 wanderCenter = wanderTarget + new Vector3(_width / 2F, 0F, _length / 2F);
        if (!IsSafeToStandAt(world, wanderCenter, avoidWater: true))
        {
            return;
        }

        SetTarget(wanderTarget);
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

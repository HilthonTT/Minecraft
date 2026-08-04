using Minecraft.Core.Entities.Player;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>
/// An entity the server steers. Only the server runs a mob's behaviour and physics; every client eases the
/// mob towards the last position and facing it was told about, the same way it does for other players.
/// </summary>
public abstract class Mob : Entity
{
    /// <summary>How close to a target counts as having arrived at it.</summary>
    private const float ArrivalDistance = 0.6F;

    /// <summary>
    /// The hop a mob makes to get over a step. The player's jump clears a block, so borrowing it means a
    /// mob can go anywhere a player walking the same route could.
    /// </summary>
    private const float JumpForce = Constants.PLAYER_JUMP_FORCE;

    private Vector3 _target;
    private bool _shouldJump;
    private int _ticksUntilNextWanderDecision;

    /// <summary>Whether the mob is currently on its way somewhere.</summary>
    protected bool HasTarget { get; private set; }

    /// <summary>
    /// Whether this is one of the mobs that comes out at night and goes for the player.
    /// <para>
    /// The spawner counts the hostile mobs and the peaceful ones against caps of their own. Sharing one cap
    /// starves out the animals: a hostile mob follows the player and so never wanders far enough off to be
    /// despawned, while an animal drifts away and is cleared, until after a night or two nothing is left but
    /// what came out of it.
    /// </para>
    /// </summary>
    public abstract bool IsHostile { get; }

    /// <summary>How hard the mob accelerates while walking.</summary>
    protected abstract float MoveSpeed { get; }

    protected Mob(int id, World? world, Vector3 position, EntityType entityType)
        : base(id, world, position, entityType)
    {
    }

    public override void Update(float deltaTime, World world)
    {
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

    /// <summary>Called every tick on the server, to choose what the mob should be doing.</summary>
    protected abstract void DecideWhatToDo(WorldServer world);

    /// <summary>Sends the mob walking towards a world position, which it re-aims at as it goes.</summary>
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

        // A yaw of zero looks along positive Z, so the components go into Atan2 the other way round from
        // the usual, which measures from positive X.
        Yaw = MathF.Atan2(toTarget.X, toTarget.Z);
        UpdateMovementBasisFromYaw();
        MoveHorizontally(0, MoveSpeed);
    }

    /// <summary>
    /// Walking into something is what tells a mob there is a step in front of it. It hops on the following
    /// frame and either clears the obstacle or, if it was more than a block tall, walks into it again.
    /// </summary>
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

    /// <summary>
    /// Sends the mob off to a random nearby spot every so often, and leaves it standing the rest of the
    /// time. Mobs with nothing better to do fall back on this.
    /// </summary>
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

        // Most decisions are to stay put, so a group of mobs does not set off as one.
        if (Random.Shared.Next(oneInChanceOfMoving) != 0)
        {
            return;
        }

        SetTarget(Position + new Vector3(
            Random.Shared.Next(-radius, radius + 1),
            0,
            Random.Shared.Next(-radius, radius + 1)));
    }

    /// <summary>The player nearest to the given point within the radius, or null when none is that close.</summary>
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

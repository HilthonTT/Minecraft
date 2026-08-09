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

    /// <summary>
    /// How long a mob is left alone after a blow, and how long it shows red for. One figure rather than
    /// two: the flash lasting exactly as long as the mob cannot be hit again is what makes it read as
    /// telling you when the next punch will land, which is what it does in the game this is modelled on.
    /// </summary>
    private const float HurtSeconds = 0.5F;

    /// <summary>How fast a blow throws a mob backwards, in blocks per second before friction eats it.</summary>
    private const float KnockbackSpeed = 7F;

    /// <summary>The lift a blow gives, as a share of the hop a mob makes to climb a step.</summary>
    private const float KnockbackLift = JumpForce * 0.45F;

    private Vector3 _target;
    private bool _shouldJump;
    private int _ticksUntilNextWanderDecision;
    private float _hurtSecondsRemaining;

    /// <summary>Whether the mob is currently on its way somewhere.</summary>
    protected bool HasTarget { get; private set; }

    /// <summary>
    /// What the mob has left. Only ever meaningful on the server: a client is told that a blow landed and
    /// whether it was the last, never what is left behind it, since nothing on that side shows a number.
    /// </summary>
    public int Health { get; private set; }

    public bool IsAlive => Health > 0;

    /// <summary>
    /// Whether the mob was hit recently enough to still be showing it. The same window is what stops the
    /// next blow from landing, so on the server this is being invulnerable and on a client it is being red.
    /// </summary>
    public bool IsHurt => _hurtSecondsRemaining > 0F;

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

    /// <summary>
    /// What it is actually walking at, which is the above unless something has it running: a frightened
    /// animal moves at a good deal more than the amble it grazes at.
    /// </summary>
    protected virtual float CurrentMoveSpeed => MoveSpeed;

    /// <summary>
    /// How much a mob of this kind can take arrives as an argument rather than as an abstract property, the
    /// way the rest of what a mob is made of does. It is needed here, in the constructor, and an override
    /// cannot be trusted to answer before the class that declares it has finished being built.
    /// </summary>
    protected Mob(int id, World? world, Vector3 position, EntityType entityType, int maxHealth)
        : base(id, world, position, entityType)
    {
        Health = maxHealth;
    }

    /// <summary>
    /// Takes a blow, on the server. Returns false when the mob is still inside the moment of grace the last
    /// one bought it, which is what stops a held mouse button from emptying a mob in a single frame.
    /// </summary>
    public bool TryHurt(int damage, Entity attacker)
    {
        if (!IsAlive || IsHurt)
        {
            return false;
        }

        Health = Math.Max(Health - damage, 0);
        ShowHurt();
        ThrowBackwardsAwayFrom(attacker.Position);
        OnHurtBy(attacker);
        return true;
    }

    /// <summary>
    /// Starts the flash without taking anything off. This is what a client does: it is told a mob was hit
    /// rather than working it out, and the health behind the blow is never sent because nothing shows it.
    /// </summary>
    public void ShowHurt() => _hurtSecondsRemaining = HurtSeconds;

    /// <summary>
    /// How the mob takes being hit. Animals bolt; a zombie takes note of who did it. Called on the server
    /// only, after the damage has been applied, so <see cref="IsAlive"/> already says whether it survived.
    /// </summary>
    protected virtual void OnHurtBy(Entity attacker)
    {
    }

    /// <summary>
    /// Throws the mob away from whatever struck it, and a little off the ground with it, so a blow reads as
    /// having landed even on something that was standing still and goes back to standing still.
    /// </summary>
    private void ThrowBackwardsAwayFrom(Vector3 source)
    {
        var away = new Vector3(Position.X - source.X, 0, Position.Z - source.Z);

        // Whatever hit it is standing exactly where it is, which leaves no direction to be thrown in, so it
        // goes over whichever way it happened to be facing.
        away = away.LengthSquared < 0.0001F ? _moveForward : away.Normalized();

        Velocity.X = away.X * KnockbackSpeed;
        Velocity.Z = away.Z * KnockbackSpeed;

        // Only off the ground it is standing on. Adding lift to one already in the air would let a mob be
        // punched up a wall a blow at a time.
        if (!_isInAir)
        {
            _verticalSpeed = KnockbackLift;
            _isInAir = true;
        }
    }

    public override void Update(float deltaTime, World world)
    {
        // Wound down on both sides of the connection: the server is counting out an invulnerability and a
        // client is counting out a flash, and they are the same half second.
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
        MoveHorizontally(0, CurrentMoveSpeed);
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

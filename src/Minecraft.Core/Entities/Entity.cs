using Minecraft.Core.Physics;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

public abstract class Entity
{
    /// <summary>The furthest an entity may move within a single collision step.</summary>
    private const float MaxDistancePerCollisionStep = 0.4F;

    /// <summary>Caps how far a single frame is split up, so an extreme speed cannot stall the frame.</summary>
    private const int MaxCollisionStepsPerFrame = 32;

    /// <summary>Slack when comparing hitbox faces, to absorb the rounding left by an earlier resolve.</summary>
    private const float CollisionTolerance = 0.001F;

    /// <summary>How far inside a block an entity has to be before it counts as stuck in it.</summary>
    private const float StuckPenetrationDepth = 0.01F;

    /// <summary>How many times per frame an entity is pushed up while stuck inside blocks.</summary>
    private const int MaxUnstickAttempts = 4;

    /// <summary>How far below the feet the ground is looked for.</summary>
    private const float GroundProbeDepth = 0.01F;

    private const float MovementFriction = -10.0F;

    /// <summary>How quickly a client side entity closes the gap to the last state the server sent.</summary>
    private const float ServerStateLerpSmoothFactor = 20;

    public int ID { get; set; }

    public EntityType EntityType { get; }

    /// <summary>
    /// Null until the entity has been placed in a world. The local player is built before the world exists,
    /// and is given one once the server accepts the connection.
    /// </summary>
    public World? World { get; set; }

    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 Acceleration;

    /// <summary>
    /// Which way the entity faces, in radians around the Y axis. Zero looks along positive Z, matching the
    /// direction <see cref="Utilities.MathUtils.CreateLookAtVector"/> produces for a yaw of zero.
    /// </summary>
    public float Yaw { get; set; }

    /// <summary>
    /// The position last reported by the server. Only meaningful on a client, for the entities the server
    /// owns and this side of the connection only draws.
    /// </summary>
    public Vector3 ServerPosition { get; set; }

    /// <summary>The facing last reported by the server, alongside <see cref="ServerPosition"/>.</summary>
    public float ServerYaw { get; set; }

    public AxisAlignedBox Hitbox { get; protected set; }

    /// <summary>The chunk the entity is currently in, or null while that chunk is not loaded.</summary>
    public Chunk? Chunk { get; private set; }

    public float Width => _width;

    public float Height => _height;

    public float Length => _length;

    protected float _width, _height, _length;

    protected bool _doCollisionDetection = true;
    protected bool _isInAir = true;
    protected float _verticalSpeed;

    /// <summary>Vector facing where the entity is heading, ignoring the vertical component.</summary>
    protected Vector3 _moveForward = Vector3.UnitZ;

    /// <summary>Perpendicular to <see cref="_moveForward"/>. Moving right means moving along its negation.</summary>
    protected Vector3 _right = Vector3.UnitX;

    private readonly Dictionary<Vector3i, BlockState> _collidableBlocks = [];

    /// <summary>Reused by the ground check, which runs every frame.</summary>
    private readonly AxisAlignedBox _groundProbeBox = new(Vector3.Zero, Vector3.Zero);

    private Vector2 _previousChunkPos = new(float.MaxValue, float.MaxValue);

    public delegate void OnDespawned();
    public event OnDespawned? OnDespawnedHandler;

    public delegate void OnChunkChanged(World world, Vector2 gridPos);
    public event OnChunkChanged? OnChunkChangedHandler;

    protected Entity(int id, World? world, Vector3 position, EntityType entityType)
    {
        ID = id;
        World = world;
        Position = position;
        Velocity = Vector3.Zero;
        Acceleration = Vector3.Zero;
        EntityType = entityType;

        SetInitialDimensions();

        Vector3 max = new(position.X + _width, position.Y + _height, position.Z + _length);
        Hitbox = new AxisAlignedBox(position, max);
    }

    public void RaiseOnDespawned() => OnDespawnedHandler?.Invoke();

    protected abstract void SetInitialDimensions();

    protected void UpdateAxisAlignedBox()
    {
        Vector3 max = new(Position.X + _width, Position.Y + _height, Position.Z + _length);
        Hitbox.SetDimensions(Position, max);
    }

    /// <summary>Called as often as possible.</summary>
    public virtual void Update(float deltaTime, World world)
    {
        UpdateAxisAlignedBox();
    }

    /// <summary>Called every tick.</summary>
    public virtual void Tick(float deltaTime, World world)
    {
        Vector2 chunkPosition = Worlds.World.GetChunkPosition(Position.X, Position.Z);
        if (_previousChunkPos != chunkPosition)
        {
            Chunk = world.LoadedChunks.TryGetValue(chunkPosition, out Chunk? newChunk) ? newChunk : null;
            OnChunkChangedHandler?.Invoke(world, chunkPosition);
        }

        _previousChunkPos = chunkPosition;
    }

    /// <summary>Whether gravity pulls the entity down. Overridden by anything that can hold itself up.</summary>
    protected virtual bool IsAffectedByGravity => true;

    /// <summary>Called whenever a horizontal move was cut short by a block.</summary>
    protected virtual void OnHorizontalCollision()
    {
    }

    /// <summary>
    /// Eases this entity towards the last state the server reported. Updates arrive an order of magnitude
    /// less often than frames are drawn, so snapping to them would make everything move in steps.
    /// </summary>
    protected void InterpolateTowardsServerState(float deltaTime)
    {
        Position = MathUtils.Lerp(Position, ServerPosition, deltaTime * ServerStateLerpSmoothFactor);
        Yaw = MathUtils.LerpAngle(Yaw, ServerYaw, deltaTime * ServerStateLerpSmoothFactor);
    }

    /// <summary>Points the movement basis along the current yaw.</summary>
    protected void UpdateMovementBasisFromYaw()
    {
        _moveForward = new Vector3(MathF.Sin(Yaw), 0, MathF.Cos(Yaw));
        _right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, _moveForward));
    }

    /// <summary>Moves horizontally relative to the direction the entity is facing. x is right, z is forward.</summary>
    protected void MoveHorizontally(float x, float z)
    {
        Acceleration += x * _right;
        Acceleration += z * _moveForward;
    }

    /// <summary>Moves vertically relative to the world up vector.</summary>
    protected void MoveVertically(float y)
    {
        Acceleration.Y += y;
    }

    protected void TryApplyGravity(float deltaTime)
    {
        if (!_isInAir)
        {
            return;
        }

        // Capped, because a fall that keeps accelerating eventually covers more ground per frame than the
        // collision check can follow.
        _verticalSpeed = MathF.Max(_verticalSpeed + Constants.GRAVITY * deltaTime, Constants.MAX_FALL_SPEED);
        MoveVertically(_verticalSpeed);
    }

    /// <summary>
    /// Integrates velocity and resolves collisions. The movement is split into steps short enough that the
    /// entity can never cross a whole block within one of them, since the collision check only looks at the
    /// blocks immediately around it: a long frame, or a fall that has been accelerating for a while, would
    /// otherwise carry the entity straight past the floor and leave it inside the world.
    /// </summary>
    protected void ApplyVelocityAndCheckCollision(float deltaTime, World world)
    {
        if (IsAffectedByGravity)
        {
            TryApplyGravity(deltaTime);
        }

        int steps = GetCollisionStepCount(deltaTime);
        float stepDeltaTime = deltaTime / steps;

        for (int step = 0; step < steps; step++)
        {
            MoveAndResolveCollisions(stepDeltaTime, world);
        }

        if (_doCollisionDetection)
        {
            PushOutOfBlocksStuckIn(world);
        }

        Velocity += MovementFriction * Velocity * deltaTime;
    }

    /// <summary>How many steps this frame's movement is split into to keep every step below a block.</summary>
    private int GetCollisionStepCount(float deltaTime)
    {
        if (!_doCollisionDetection)
        {
            return 1;
        }

        // The velocity at the end of the frame, which is the highest it gets while accelerating, so this
        // never underestimates the distance covered.
        Vector3 endOfFrameVelocity = Velocity + Acceleration * deltaTime;
        float distance = endOfFrameVelocity.Length * deltaTime;

        var steps = (int)MathF.Ceiling(distance / MaxDistancePerCollisionStep);
        return Math.Clamp(steps, 1, MaxCollisionStepsPerFrame);
    }

    /// <summary>
    /// Moves by a single step. Each axis is moved and resolved on its own, so that sliding along a wall does
    /// not also stop movement along the other axes.
    /// </summary>
    private void MoveAndResolveCollisions(float deltaTime, World world)
    {
        Velocity += Acceleration * deltaTime;

        if (!_doCollisionDetection)
        {
            Position += Velocity * deltaTime;
            UpdateAxisAlignedBox();
            return;
        }

        // Gathered before moving. A step covers less than a block, so the margin around the hitbox still
        // reaches everything the entity can run into within it.
        Dictionary<Vector3i, BlockState> blocks = GetCollisionDetectionBlocks(world);

        float previousX = Position.X;
        Position.X += Velocity.X * deltaTime;
        UpdateAxisAlignedBox();
        DoXAxisCollisionDetection(blocks, previousX);

        float previousY = Position.Y;
        Position.Y += Velocity.Y * deltaTime;
        UpdateAxisAlignedBox();
        DoYAxisCollisionDetection(blocks, previousY);

        float previousZ = Position.Z;
        Position.Z += Velocity.Z * deltaTime;
        UpdateAxisAlignedBox();
        DoZAxisCollisionDetection(blocks, previousZ);
    }

    /// <summary>
    /// Returns the blocks around the entity used for collision detection. The range comes from the hitbox
    /// rather than the position, so that the whole body is covered whichever way the entity is moving, plus
    /// a block of margin on every side for the movement done within a step.
    /// </summary>
    private Dictionary<Vector3i, BlockState> GetCollisionDetectionBlocks(World world)
    {
        _collidableBlocks.Clear();

        Vector3i min = Hitbox.Min.ToBlockPos() - Vector3i.One;
        Vector3i max = Hitbox.Max.ToBlockPos() + Vector3i.One;

        for (int worldX = min.X; worldX <= max.X; worldX++)
        {
            for (int worldY = min.Y; worldY <= max.Y; worldY++)
            {
                for (int worldZ = min.Z; worldZ <= max.Z; worldZ++)
                {
                    var blockPos = new Vector3i(worldX, worldY, worldZ);
                    BlockState blockState = world.GetBlockAt(blockPos);
                    if (blockState.GetBlock() != BlockRegistry.Air)
                    {
                        _collidableBlocks.Add(blockPos, blockState);
                    }
                }
            }
        }

        return _collidableBlocks;
    }

    private void DoXAxisCollisionDetection(Dictionary<Vector3i, BlockState> blocks, float previousX)
    {
        foreach (KeyValuePair<Vector3i, BlockState> collidable in blocks)
        {
            foreach (AxisAlignedBox aabb in collidable.Value.GetBlock().GetCollisionBox(collidable.Value, collidable.Key))
            {
                if (!Hitbox.Intersects(aabb))
                {
                    continue;
                }

                if (Velocity.X > 0.0F && previousX + _width <= aabb.Min.X + CollisionTolerance)
                {
                    Position.X = aabb.Min.X - _width;
                }
                else if (Velocity.X < 0.0F && previousX >= aabb.Max.X - CollisionTolerance)
                {
                    Position.X = aabb.Max.X;
                }
                else
                {
                    // The entity was already inside this block before the step. Snapping to whichever face
                    // its velocity points at would push it further into the world instead of out of it, so
                    // getting it out is left to the unstick pass.
                    continue;
                }

                Velocity.X = 0.0F;
                UpdateAxisAlignedBox();
                OnHorizontalCollision();
            }
        }
    }

    private void DoYAxisCollisionDetection(Dictionary<Vector3i, BlockState> blocks, float previousY)
    {
        bool landed = false;

        foreach (KeyValuePair<Vector3i, BlockState> collidable in blocks)
        {
            foreach (AxisAlignedBox aabb in collidable.Value.GetBlock().GetCollisionBox(collidable.Value, collidable.Key))
            {
                if (!Hitbox.Intersects(aabb))
                {
                    continue;
                }

                if (Velocity.Y > 0.0F && previousY + _height <= aabb.Min.Y + CollisionTolerance)
                {
                    Position.Y = aabb.Min.Y - _height;
                }
                else if (Velocity.Y < 0.0F && previousY >= aabb.Max.Y - CollisionTolerance)
                {
                    Position.Y = aabb.Max.Y;
                    landed = true;
                }
                else
                {
                    continue;
                }

                Velocity.Y = 0.0F;
                _verticalSpeed = 0.0F;
                UpdateAxisAlignedBox();
            }
        }

        // An entity moving upwards is airborne no matter what is under it. On a short frame the first step
        // of a jump lifts it by less than the ground is probed for, and looking for it would put it back on
        // the ground before it ever left it.
        _isInAir = !landed && (Velocity.Y > 0.0F || !IsStandingOnGround(blocks));
    }

    private void DoZAxisCollisionDetection(Dictionary<Vector3i, BlockState> blocks, float previousZ)
    {
        foreach (KeyValuePair<Vector3i, BlockState> collidable in blocks)
        {
            foreach (AxisAlignedBox aabb in collidable.Value.GetBlock().GetCollisionBox(collidable.Value, collidable.Key))
            {
                if (!Hitbox.Intersects(aabb))
                {
                    continue;
                }

                if (Velocity.Z > 0.0F && previousZ + _length <= aabb.Min.Z + CollisionTolerance)
                {
                    Position.Z = aabb.Min.Z - _length;
                }
                else if (Velocity.Z < 0.0F && previousZ >= aabb.Max.Z - CollisionTolerance)
                {
                    Position.Z = aabb.Max.Z;
                }
                else
                {
                    continue;
                }

                Velocity.Z = 0.0F;
                UpdateAxisAlignedBox();
                OnHorizontalCollision();
            }
        }
    }

    /// <summary>
    /// Whether something solid sits directly under the entity's feet. Resolving the vertical move is not
    /// enough on its own to tell: an entity standing still never moves into the ground, and so would be
    /// considered airborne every other frame.
    /// </summary>
    private bool IsStandingOnGround(Dictionary<Vector3i, BlockState> blocks)
    {
        _groundProbeBox.SetDimensions(
            new Vector3(Hitbox.Min.X, Hitbox.Min.Y - GroundProbeDepth, Hitbox.Min.Z),
            new Vector3(Hitbox.Max.X, Hitbox.Min.Y, Hitbox.Max.Z));

        foreach (KeyValuePair<Vector3i, BlockState> collidable in blocks)
        {
            foreach (AxisAlignedBox aabb in collidable.Value.GetBlock().GetCollisionBox(collidable.Value, collidable.Key))
            {
                if (_groundProbeBox.Intersects(aabb))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Lifts the entity onto the block it ended up inside of. Moving no longer puts it there, but a chunk
    /// loading around it or a block appearing where it stands still can, and without this it is trapped:
    /// every direction it could move into is solid.
    /// </summary>
    private void PushOutOfBlocksStuckIn(World world)
    {
        for (int attempt = 0; attempt < MaxUnstickAttempts; attempt++)
        {
            float highestBlockTop = float.MinValue;

            foreach (KeyValuePair<Vector3i, BlockState> collidable in GetCollisionDetectionBlocks(world))
            {
                foreach (AxisAlignedBox aabb in collidable.Value.GetBlock().GetCollisionBox(collidable.Value, collidable.Key))
                {
                    if (IsStuckInside(aabb))
                    {
                        highestBlockTop = MathF.Max(highestBlockTop, aabb.Max.Y);
                    }
                }
            }

            if (highestBlockTop == float.MinValue)
            {
                return;
            }

            Position.Y = highestBlockTop;
            Velocity.Y = 0.0F;
            _verticalSpeed = 0.0F;
            _isInAir = false;
            UpdateAxisAlignedBox();
        }
    }

    /// <summary>
    /// Whether the entity is inside the given box by more than the rounding a resolved collision leaves
    /// behind. Touching a face, or overlapping it by a fraction, is not being stuck.
    /// </summary>
    private bool IsStuckInside(AxisAlignedBox aabb)
    {
        return Hitbox.Min.X < aabb.Max.X - StuckPenetrationDepth && Hitbox.Max.X > aabb.Min.X + StuckPenetrationDepth &&
               Hitbox.Min.Y < aabb.Max.Y - StuckPenetrationDepth && Hitbox.Max.Y > aabb.Min.Y + StuckPenetrationDepth &&
               Hitbox.Min.Z < aabb.Max.Z - StuckPenetrationDepth && Hitbox.Max.Z > aabb.Min.Z + StuckPenetrationDepth;
    }
}

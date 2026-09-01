using Minecraft.Core.Physics;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.Types;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

public abstract class Entity
{
    private const float MaxDistancePerCollisionStep = 0.4F;

    private const int MaxCollisionStepsPerFrame = 32;

    private const float CollisionTolerance = 0.001F;

    private const float StuckPenetrationDepth = 0.01F;

    private const int MaxUnstickAttempts = 4;

    private const float GroundProbeDepth = 0.01F;

    private static readonly Vector3i[] _liquidFlowSideOffsets =
    [
        Vector3iExtensions.NorthBasis,
        Vector3iExtensions.SouthBasis,
        Vector3iExtensions.EastBasis,
        Vector3iExtensions.WestBasis,
    ];

    private const float MovementFriction = -10.0F;

    private const float ServerStateLerpSmoothFactor = 20;

    public int ID { get; set; }

    public EntityType EntityType { get; }

    public World? World { get; set; }

    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 Acceleration;

    public float Yaw { get; set; }

    public Vector3 ServerPosition { get; set; }

    public float ServerYaw { get; set; }

    public AxisAlignedBox Hitbox { get; protected set; }

    public Chunk? Chunk { get; private set; }

    public float Width => _width;

    public float Height => _height;

    public float Length => _length;

    protected float _width, _height, _length;

    protected bool _doCollisionDetection = true;
    protected bool _isInAir = true;
    protected float _verticalSpeed;

    protected bool _isInLiquid;

    protected Vector3 _liquidFlow;

    public bool IsOnGround => !_isInAir;

    public bool IsInLiquid => _isInLiquid;

    protected Vector3 _moveForward = Vector3.UnitZ;

    protected Vector3 _right = Vector3.UnitX;

    private readonly Dictionary<Vector3i, BlockState> _collidableBlocks = [];

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

    protected void ForgetCurrentChunk()
    {
        Chunk = null;
        _previousChunkPos = new Vector2(float.MaxValue, float.MaxValue);
    }

    protected abstract void SetInitialDimensions();

    protected void UpdateAxisAlignedBox()
    {
        Vector3 max = new(Position.X + _width, Position.Y + _height, Position.Z + _length);
        Hitbox.SetDimensions(Position, max);
    }

    public virtual void Update(float deltaTime, World world)
    {
        UpdateAxisAlignedBox();
    }

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

    protected virtual bool IsAffectedByGravity => true;

    protected virtual void OnHorizontalCollision()
    {
    }

    protected void InterpolateTowardsServerState(float deltaTime)
    {
        Position = MathUtils.Lerp(Position, ServerPosition, deltaTime * ServerStateLerpSmoothFactor);
        Yaw = MathUtils.LerpAngle(Yaw, ServerYaw, deltaTime * ServerStateLerpSmoothFactor);
    }

    protected void UpdateMovementBasisFromYaw()
    {
        _moveForward = new Vector3(MathF.Sin(Yaw), 0, MathF.Cos(Yaw));
        _right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, _moveForward));
    }

    protected void MoveHorizontally(float x, float z)
    {
        Acceleration += x * _right;
        Acceleration += z * _moveForward;
    }

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

        if (_isInLiquid)
        {
            _verticalSpeed = MathF.Max(
                _verticalSpeed + (Constants.GRAVITY * Constants.WATER_GRAVITY_MULTIPLIER * deltaTime),
                Constants.MAX_SINK_SPEED);

            MoveVertically(_verticalSpeed);
            return;
        }

        _verticalSpeed = MathF.Max(_verticalSpeed + Constants.GRAVITY * deltaTime, Constants.MAX_FALL_SPEED);
        MoveVertically(_verticalSpeed);
    }

    protected void ApplyVelocityAndCheckCollision(float deltaTime, World world)
    {
        UpdateLiquidState(world);

        if (IsAffectedByGravity)
        {
            TryApplyGravity(deltaTime);
        }

        Velocity += _liquidFlow * Constants.WATER_PUSH_FORCE * deltaTime;

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

    private void UpdateLiquidState(World world)
    {
        var samplePos = new Vector3i(
            (int)MathF.Floor(Position.X + (_width / 2F)),
            (int)MathF.Floor(Position.Y + (_height / 2F)),
            (int)MathF.Floor(Position.Z + (_length / 2F)));

        if (world.IsOutsideBuildHeight(samplePos.Y))
        {
            _isInLiquid = false;
            _liquidFlow = Vector3.Zero;
            return;
        }

        _isInLiquid = world.GetBlockAt(samplePos).GetBlock().IsLiquid;
        _liquidFlow = _isInLiquid ? GetLiquidFlowAt(world, samplePos) : Vector3.Zero;
    }

    private static Vector3 GetLiquidFlowAt(World world, Vector3i blockPos)
    {
        if (world.GetBlockAt(blockPos).GetBlock() is not BlockWater water)
        {
            return Vector3.Zero;
        }

        var flow = Vector3.Zero;

        foreach (Vector3i sideOffset in _liquidFlowSideOffsets)
        {
            Vector3i sidePos = blockPos + sideOffset;
            BlockState sideState = world.GetBlockAt(sidePos);
            Block side = sideState.GetBlock();

            float sideHeight;
            if (side is BlockWater sideWater)
            {
                sideHeight = sideWater.SurfaceHeight;
            }
            else if (side.GetCollisionBox(sideState, sidePos).Length == 0)
            {
                sideHeight = 0F;
            }
            else
            {
                sideHeight = water.SurfaceHeight;
            }

            flow += new Vector3(sideOffset.X, 0, sideOffset.Z) * (water.SurfaceHeight - sideHeight);
        }

        return flow.LengthSquared > 0.0001F ? Vector3.Normalize(flow) : Vector3.Zero;
    }

    private int GetCollisionStepCount(float deltaTime)
    {
        if (!_doCollisionDetection)
        {
            return 1;
        }

        Vector3 endOfFrameVelocity = Velocity + Acceleration * deltaTime;
        float distance = endOfFrameVelocity.Length * deltaTime;

        var steps = (int)MathF.Ceiling(distance / MaxDistancePerCollisionStep);
        return Math.Clamp(steps, 1, MaxCollisionStepsPerFrame);
    }

    private void MoveAndResolveCollisions(float deltaTime, World world)
    {
        Velocity += Acceleration * deltaTime;

        if (!_doCollisionDetection)
        {
            Position += Velocity * deltaTime;
            UpdateAxisAlignedBox();
            return;
        }

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

    private bool IsStuckInside(AxisAlignedBox aabb)
    {
        return Hitbox.Min.X < aabb.Max.X - StuckPenetrationDepth && Hitbox.Max.X > aabb.Min.X + StuckPenetrationDepth &&
               Hitbox.Min.Y < aabb.Max.Y - StuckPenetrationDepth && Hitbox.Max.Y > aabb.Min.Y + StuckPenetrationDepth &&
               Hitbox.Min.Z < aabb.Max.Z - StuckPenetrationDepth && Hitbox.Max.Z > aabb.Min.Z + StuckPenetrationDepth;
    }
}

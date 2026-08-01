using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Minecraft.Core.Entities.Player;

public abstract class Player : Entity
{
    protected const int MaxBlockReach = 40;

    /// <summary>Two jumps within this window toggle flight.</summary>
    private const long DoubleJumpWindowMilliseconds = 300;

    private const float MovementFriction = -10.0F;

    /// <summary>The furthest the player may move within a single collision step.</summary>
    private const float MaxDistancePerCollisionStep = 0.4F;

    /// <summary>Caps how far a single frame is split up, so an extreme speed cannot stall the frame.</summary>
    private const int MaxCollisionStepsPerFrame = 32;

    /// <summary>Slack when comparing hitbox faces, to absorb the rounding left by an earlier resolve.</summary>
    private const float CollisionTolerance = 0.001F;

    /// <summary>How far inside a block the player has to be before they count as stuck in it.</summary>
    private const float StuckPenetrationDepth = 0.01F;

    /// <summary>How many times per frame the player is pushed up while stuck inside blocks.</summary>
    private const int MaxUnstickAttempts = 4;

    /// <summary>How far below the feet the ground is looked for.</summary>
    private const float GroundProbeDepth = 0.01F;

    public string Name { get; set; }

    protected bool _isFlying;
    protected bool _isInCreativeMode = true;
    protected bool _doCollisionDetection = true;
    protected bool _isInAir = true;
    protected bool _isCrouching;
    protected bool _isRunning;

    protected readonly Stopwatch _jumpStopWatch = new();

    /// <summary>Vector facing towards where the player is looking.</summary>
    protected Vector3 _realForward;

    /// <summary>Vector facing where the player is looking, ignoring the vertical component.</summary>
    protected Vector3 _moveForward;

    /// <summary>Vector facing to the right of where the player is looking.</summary>
    protected Vector3 _right;

    protected float _verticalSpeed;

    private readonly Dictionary<Vector3i, BlockState> _collidableBlocks = [];

    /// <summary>Reused by the ground check, which runs every frame.</summary>
    private readonly AxisAlignedBox _groundProbeBox = new(Vector3.Zero, Vector3.Zero);

    protected delegate void OnToggleRunning(bool isRunning);
    protected event OnToggleRunning? OnToggleRunningHandler;

    protected delegate void OnToggleCrouching(bool isCrouching);
    protected event OnToggleCrouching? OnToggleCrouchingHandler;

    protected Player(int id, string playerName, World? world, Vector3 startPosition)
        : base(id, world, startPosition, EntityType.Player)
    {
        Name = playerName;
        _jumpStopWatch.Start();
    }

    protected override void SetInitialDimensions()
    {
        _width = Constants.PLAYER_WIDTH;
        _height = Constants.PLAYER_HEIGHT;
        _length = Constants.PLAYER_LENGTH;
    }

    /// <summary>Moves horizontally relative to the direction the player is facing. x is right, z is forward.</summary>
    protected void MovePlayerHorizontally(float x, float z)
    {
        Acceleration += x * _right;
        Acceleration += z * _moveForward;
    }

    /// <summary>Moves vertically relative to the world up vector.</summary>
    protected void MovePlayerVertically(float y)
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
        MovePlayerVertically(_verticalSpeed);
    }

    protected void TryStartRunning()
    {
        if (_isRunning || (!_isInCreativeMode && _isInAir))
        {
            return;
        }

        _isRunning = true;
        OnToggleRunningHandler?.Invoke(_isRunning);
    }

    protected void TryStopRunning()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        OnToggleRunningHandler?.Invoke(_isRunning);
    }

    protected void TryToggleFlying()
    {
        _jumpStopWatch.Stop();
        if (_jumpStopWatch.ElapsedMilliseconds < DoubleJumpWindowMilliseconds && _isInCreativeMode)
        {
            _isFlying = !_isFlying;
            _verticalSpeed = 0;
        }
        _jumpStopWatch.Restart();
    }

    protected void TryStopCrouching()
    {
        if (!_isCrouching)
        {
            return;
        }

        _isCrouching = false;
        OnToggleCrouchingHandler?.Invoke(_isCrouching);
    }

    protected void TryStartCrouching()
    {
        if (_isRunning)
        {
            TryStopRunning();
        }

        _isCrouching = true;
        OnToggleCrouchingHandler?.Invoke(_isCrouching);
    }

    protected void AttemptToJump()
    {
        if (_isInAir)
        {
            return;
        }

        _verticalSpeed = Constants.PLAYER_JUMP_FORCE;
        _isInAir = true;
    }

    /// <summary>
    /// Integrates velocity and resolves collisions. The movement is split into steps short enough that the
    /// player can never cross a whole block within one of them, since the collision check only looks at the
    /// blocks immediately around them: a long frame, or a fall that has been accelerating for a while, would
    /// otherwise carry the player straight past the floor and leave them inside the world.
    /// </summary>
    protected void ApplyVelocityAndCheckCollision(float deltaTime, World world)
    {
        if (!_isFlying)
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
            PushOutOfBlocksPlayerIsStuckIn(world);
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
        // reaches everything the player can run into within it.
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
    /// Returns the blocks around the player used for collision detection. The range comes from the hitbox
    /// rather than the position, so that the whole body is covered whichever way the player is moving, plus
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
                    // The player was already inside this block before the step. Snapping to whichever face
                    // their velocity points at would push them further into the world instead of out of it,
                    // so getting them out is left to the unstick pass.
                    continue;
                }

                Velocity.X = 0.0F;
                UpdateAxisAlignedBox();
                TryStopRunning();
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

        // A player moving upwards is airborne no matter what is under them. On a short frame the first step
        // of a jump lifts them by less than the ground is probed for, and looking for it would put them back
        // on the ground before they ever left it.
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
                TryStopRunning();
            }
        }
    }

    /// <summary>
    /// Whether something solid sits directly under the player's feet. Resolving the vertical move is not
    /// enough on its own to tell: a player standing still never moves into the ground, and so would be
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
    /// Lifts the player onto the block they ended up inside of. Moving no longer puts them there, but a
    /// chunk loading around them or a block appearing where they stand still can, and without this they are
    /// trapped: every direction they could move into is solid.
    /// </summary>
    private void PushOutOfBlocksPlayerIsStuckIn(World world)
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
    /// Whether the player is inside the given box by more than the rounding a resolved collision leaves
    /// behind. Touching a face, or overlapping it by a fraction, is not being stuck.
    /// </summary>
    private bool IsStuckInside(AxisAlignedBox aabb)
    {
        return Hitbox.Min.X < aabb.Max.X - StuckPenetrationDepth && Hitbox.Max.X > aabb.Min.X + StuckPenetrationDepth &&
               Hitbox.Min.Y < aabb.Max.Y - StuckPenetrationDepth && Hitbox.Max.Y > aabb.Min.Y + StuckPenetrationDepth &&
               Hitbox.Min.Z < aabb.Max.Z - StuckPenetrationDepth && Hitbox.Max.Z > aabb.Min.Z + StuckPenetrationDepth;
    }
}

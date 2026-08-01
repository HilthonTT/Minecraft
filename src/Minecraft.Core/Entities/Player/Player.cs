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

        _verticalSpeed += Constants.GRAVITY * deltaTime;
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
    /// Integrates velocity and resolves collisions. Each axis is moved and resolved on its own, so that
    /// sliding along a wall does not also stop movement along the other axes.
    /// </summary>
    protected void ApplyVelocityAndCheckCollision(float deltaTime, World world)
    {
        if (!_isFlying)
        {
            TryApplyGravity(deltaTime);
        }

        Dictionary<Vector3i, BlockState>? blocks = null;
        if (_doCollisionDetection)
        {
            blocks = GetCollisionDetectionBlocks(world);
        }

        Velocity.X += Acceleration.X * deltaTime;
        Position.X += Velocity.X * deltaTime;
        UpdateAxisAlignedBox();
        if (blocks != null)
        {
            DoXAxisCollisionDetection(blocks);
        }

        Velocity.Y += Acceleration.Y * deltaTime;
        Position.Y += Velocity.Y * deltaTime;
        UpdateAxisAlignedBox();
        if (blocks != null)
        {
            DoYAxisCollisionDetection(blocks);
        }

        Velocity.Z += Acceleration.Z * deltaTime;
        Position.Z += Velocity.Z * deltaTime;
        UpdateAxisAlignedBox();
        if (blocks != null)
        {
            DoZAxisCollisionDetection(blocks);
        }

        Velocity += MovementFriction * Velocity * deltaTime;
    }

    /// <summary>Returns the blocks around the player's position used for collision detection.</summary>
    private Dictionary<Vector3i, BlockState> GetCollisionDetectionBlocks(World world)
    {
        _collidableBlocks.Clear();

        Vector3i pos = Position.ToBlockPos();
        int topY = pos.Y + (int)Math.Ceiling(Constants.PLAYER_HEIGHT);

        for (int worldX = pos.X - 1; worldX <= pos.X + 1; worldX++)
        {
            for (int worldY = pos.Y - 1; worldY <= topY; worldY++)
            {
                for (int worldZ = pos.Z - 1; worldZ <= pos.Z + 1; worldZ++)
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

    private void DoXAxisCollisionDetection(Dictionary<Vector3i, BlockState> blocks)
    {
        foreach (KeyValuePair<Vector3i, BlockState> collidable in blocks)
        {
            foreach (AxisAlignedBox aabb in collidable.Value.GetBlock().GetCollisionBox(collidable.Value, collidable.Key))
            {
                if (!Hitbox.Intersects(aabb))
                {
                    continue;
                }

                if (Velocity.X > 0.0F)
                {
                    Position.X = aabb.Min.X - Constants.PLAYER_WIDTH;
                }
                else if (Velocity.X < 0.0F)
                {
                    Position.X = aabb.Max.X;
                }

                Velocity.X = 0.0F;
                TryStopRunning();
            }
        }
    }

    private void DoYAxisCollisionDetection(Dictionary<Vector3i, BlockState> blocks)
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

                if (Velocity.Y > 0.0F)
                {
                    Position.Y = aabb.Min.Y - Constants.PLAYER_HEIGHT;
                }
                else if (Velocity.Y < 0.0F)
                {
                    Position.Y = aabb.Max.Y;
                    landed = true;
                }

                Velocity.Y = 0.0F;
                _verticalSpeed = 0.0F;
            }
        }

        _isInAir = !landed;
    }

    private void DoZAxisCollisionDetection(Dictionary<Vector3i, BlockState> blocks)
    {
        foreach (KeyValuePair<Vector3i, BlockState> collidable in blocks)
        {
            foreach (AxisAlignedBox aabb in collidable.Value.GetBlock().GetCollisionBox(collidable.Value, collidable.Key))
            {
                if (!Hitbox.Intersects(aabb))
                {
                    continue;
                }

                if (Velocity.Z > 0.0F)
                {
                    Position.Z = aabb.Min.Z - Constants.PLAYER_LENGTH;
                }
                else if (Velocity.Z < 0.0F)
                {
                    Position.Z = aabb.Max.Z;
                }

                Velocity.Z = 0.0F;
                TryStopRunning();
            }
        }
    }
}

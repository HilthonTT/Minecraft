using Minecraft.Core.Worlds;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Minecraft.Core.Entities.Player;

public abstract class Player : Entity
{
    protected const int MaxBlockReach = 40;

    /// <summary>Two jumps within this window toggle flight.</summary>
    private const long DoubleJumpWindowMilliseconds = 300;

    public string Name { get; set; }

    protected bool _isFlying;
    protected bool _isInCreativeMode = true;
    protected bool _isCrouching;
    protected bool _isRunning;

    protected readonly Stopwatch _jumpStopWatch = new();

    /// <summary>Vector facing towards where the player is looking.</summary>
    protected Vector3 _realForward;

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

    /// <summary>A flying player holds themselves up, so nothing pulls them down.</summary>
    protected override bool IsAffectedByGravity => !_isFlying;

    /// <summary>Running into a wall is the one thing that stops a sprint without letting go of the key.</summary>
    protected override void OnHorizontalCollision() => TryStopRunning();

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
}

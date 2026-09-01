using Minecraft.Core.Games;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Minecraft.Core.Entities.Player;

public abstract class Player : Entity
{
    private const int CreativeBlockReach = 40;

    private const int SurvivalBlockReach = 5;

    protected int MaxBlockReach => IsCreative ? CreativeBlockReach : SurvivalBlockReach;

    private const long DoubleJumpWindowMilliseconds = 300;

    public string Name { get; set; }

    protected bool _isFlying;
    protected bool _isCrouching;
    protected bool _isRunning;

    public GameMode GameMode { get; private set; } = GameMode.Survival;

    public bool IsCreative => GameMode == GameMode.Creative;

    public virtual void SetGameMode(GameMode gameMode)
    {
        GameMode = gameMode;

        if (gameMode != GameMode.Creative)
        {
            _isFlying = false;
        }
    }

    public bool IsRunning => _isRunning;

    public bool IsFlying => _isFlying;

    protected readonly Stopwatch _jumpStopWatch = new();

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

    protected override bool IsAffectedByGravity => !_isFlying;

    protected override void OnHorizontalCollision() => TryStopRunning();

    protected void TryStartRunning()
    {
        if (_isRunning || (!IsCreative && _isInAir))
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
        if (_jumpStopWatch.ElapsedMilliseconds < DoubleJumpWindowMilliseconds && IsCreative)
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

    protected void ResetMovementState()
    {
        TryStopRunning();
        TryStopCrouching();

        _isFlying = false;
        _isInAir = true;
        _verticalSpeed = 0;
        _jumpStopWatch.Restart();
    }

    protected void AttemptToJump()
    {
        if (_isInLiquid)
        {
            _verticalSpeed = Constants.SWIM_UP_FORCE;
            _isInAir = true;
            return;
        }

        if (_isInAir)
        {
            return;
        }

        _verticalSpeed = Constants.PLAYER_JUMP_FORCE;
        _isInAir = true;
    }
}

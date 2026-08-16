using Minecraft.Core.Games;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Minecraft.Core.Entities.Player;

public abstract class Player : Entity
{
    /// <summary>
    /// How far a creative player can reach a block, in blocks. Far beyond an arm's length on purpose: what
    /// this mode is for is laying out something large, and half of that is done from a distance.
    /// </summary>
    private const int CreativeBlockReach = 40;

    /// <summary>
    /// The same in survival, which is Minecraft's own reach. Short, and it has to be: a block broken is a
    /// block that falls where it stood, and one broken from forty blocks away would be one nobody could
    /// pick up. Reaching a thing and collecting it are the same distance, so they are the same number.
    /// </summary>
    private const int SurvivalBlockReach = 5;

    /// <summary>How far this player can reach a block, which is one of the two figures above.</summary>
    protected int MaxBlockReach => IsCreative ? CreativeBlockReach : SurvivalBlockReach;

    /// <summary>Two jumps within this window toggle flight.</summary>
    private const long DoubleJumpWindowMilliseconds = 300;

    public string Name { get; set; }

    protected bool _isFlying;
    protected bool _isCrouching;
    protected bool _isRunning;

    /// <summary>
    /// Which of the two ways this player is playing. The server owns it — a client is told what it is on the
    /// way in and again whenever it changes — so both sides read the same field and neither decides it alone.
    /// </summary>
    public GameMode GameMode { get; private set; } = GameMode.Survival;

    public bool IsCreative => GameMode == GameMode.Creative;

    /// <summary>
    /// Puts the player into the given mode. Leaving creative also puts them back on the ground: flight is
    /// creative's alone, and somebody switched to survival mid-air would otherwise stay hanging there.
    /// </summary>
    public virtual void SetGameMode(GameMode gameMode)
    {
        GameMode = gameMode;

        if (gameMode != GameMode.Creative)
        {
            _isFlying = false;
        }
    }

    /// <summary>Whether the player is sprinting, which is the one thing that kicks dust up off the ground.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>Whether the player is flying, which is not walking however fast the ground goes past.</summary>
    public bool IsFlying => _isFlying;

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

    /// <summary>
    /// Puts the movement state back to standing still on the ground. Goes through the same calls the
    /// controls do, so that whatever reacts to running or crouching - the field of view, for one - is told
    /// about it rather than left showing the last world's state.
    /// </summary>
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
        // In water the same key is a swim stroke instead, which works whether or not there is anything
        // underfoot: that is the whole of what it means to be swimming rather than jumping.
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

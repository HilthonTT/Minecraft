using Minecraft.Core.Games;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Physics;
using Minecraft.Core.Render;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Entities.Player;

/// <summary>
/// The player this client controls. Everything it does is a request to the server, which is what actually
/// changes the world.
/// </summary>
public sealed class ClientPlayer : Player
{
    private const float SecondsPerPositionUpdate = 0.1F;

    /// <summary>
    /// What sprinting and crouching do to the field of view, as a share of whatever it is resting at. Held as
    /// a multiplier rather than as an angle so that a player who has widened their view still gets the same
    /// pull when they break into a run.
    /// </summary>
    private const float RunningFieldOfViewMultiplier = 1.10F;
    private const float CrouchingFieldOfViewMultiplier = 0.97F;

    private readonly Game _game;

    private float _elapsedSecondsSinceLastPositionUpdate;

    public Camera Camera { get; }

    /// <summary>The block a right click would place. Also what is drawn in the player's own hand.</summary>
    public BlockState SelectedBlock { get; private set; } = BlockRegistry.GetState(BlockRegistry.Tnt);

    /// <summary>
    /// Raised when the player swings at the world — breaking, placing or interacting. Watched by the renderer
    /// that draws the held block, which is the only thing an arm swing is visible in.
    /// </summary>
    public event Action? OnSwingHandler;

    /// <summary>The block the player is currently looking at, or null when out of reach.</summary>
    public RayTraceResult? MouseOverObject { get; private set; }

    public ClientPlayer(Game game) : base(-1, string.Empty, null, new Vector3(-1, -1, -1))
    {
        _game = game;

        Camera = new Camera(new ProjectionMatrixInfo
        {
            DistanceNearPlane = 0.1F,
            DistanceFarPlane = 1000F,
            FieldOfView = game.Settings.FieldOfViewRadians,
            WindowPixelWidth = game.Window.ClientSize.X,
            WindowPixelHeight = game.Window.ClientSize.Y,
        });

        OnToggleRunningHandler += OnRunningToggle;
        OnToggleCrouchingHandler += OnCrouchingToggle;
    }

    /// <summary>Takes the field of view the player has chosen, for when they change it from the options.</summary>
    public void ApplyFieldOfViewSetting()
    {
        Camera.SetDefaultFieldOfView(_game.Settings.FieldOfViewRadians);
    }

    /// <summary>
    /// Puts the player back to how it was before it ever joined a world. The instance itself is kept, since
    /// the camera and everything the renderer holds are built around this one, so the next world starts from
    /// a clean state rather than inheriting where the last one was left.
    /// </summary>
    public void ResetForNewSession()
    {
        // The identity the server hands out on joining. Until then this is not an entity any world knows.
        ID = -1;
        Name = string.Empty;
        World = null;

        Position = new Vector3(-1, -1, -1);
        Velocity = Vector3.Zero;
        Acceleration = Vector3.Zero;
        ServerPosition = Position;
        Yaw = 0;
        ServerYaw = 0;

        MouseOverObject = null;
        _elapsedSecondsSinceLastPositionUpdate = 0;

        ResetMovementState();
        ForgetCurrentChunk();
        UpdateAxisAlignedBox();
    }

    private void OnRunningToggle(bool isRunning)
    {
        if (isRunning)
        {
            Camera.SetFieldOfView(Camera.DefaultFieldOfView * RunningFieldOfViewMultiplier);
        }
        else
        {
            Camera.SetFieldOfViewToDefault();
        }
    }

    private void OnCrouchingToggle(bool isCrouching)
    {
        if (isCrouching)
        {
            Camera.SetFieldOfView(Camera.DefaultFieldOfView * CrouchingFieldOfViewMultiplier);
        }
        else
        {
            Camera.SetFieldOfViewToDefault();
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 cameraPosition = Position;
        cameraPosition.X += Constants.PLAYER_WIDTH / 2.0F;
        cameraPosition.Y += Constants.PLAYER_CAMERA_HEIGHT;
        cameraPosition.Z += Constants.PLAYER_LENGTH / 2.0F;
        Camera.SetPosition(cameraPosition);
    }

    public override void Update(float deltaTime, World world)
    {
        Acceleration = Vector3.Zero;

        if (_game.IsGameplayInputEnabled)
        {
            UpdateKeyboardMovementInput();
        }

        if (_game.IsGameplayInputEnabled)
        {
            UpdateBlockSelectionInput();
        }

        ApplyVelocityAndCheckCollision(deltaTime, world);
        MouseOverObject = new Ray(Camera.Position, Camera.Forward).TraceWorld(world, MaxBlockReach);

        UpdateCameraPosition();
        UpdateMouseInput(world);

        _realForward = Camera.Forward;
        // The movement basis ignores pitch, so looking up does not slow the player down.
        Yaw = Camera.Yaw;
        UpdateMovementBasisFromYaw();

        _elapsedSecondsSinceLastPositionUpdate += deltaTime;
        if (_elapsedSecondsSinceLastPositionUpdate > SecondsPerPositionUpdate)
        {
            _elapsedSecondsSinceLastPositionUpdate = 0;
            _game.Client.WritePacket(new EntityDataPacket(ID, Position, Velocity, Yaw));
        }
    }

    private void UpdateMouseInput(World world)
    {
        // A click while the chat or a menu is open belongs to it, not to the block being looked at.
        if (MouseOverObject is null || !_game.Window.IsFocused || !_game.IsGameplayInputEnabled)
        {
            return;
        }

        if (Game.Input.OnMousePress(MouseButton.Right))
        {
            Block hitBlock = world.GetBlockAt(MouseOverObject.IntersectedBlockPos).GetBlock();
            Block selected = SelectedBlock.GetBlock();

            OnSwingHandler?.Invoke();

            if (!_isCrouching && hitBlock.IsInteractable)
            {
                _game.Client.WritePacket(new PlayerBlockInteractionPacket(MouseOverObject.IntersectedBlockPos));

                // Played here rather than waiting to be told, since the fuse is lit by this click and the
                // server broadcasts nothing until the blast itself.
                if (hitBlock == BlockRegistry.Tnt)
                {
                    _game.SoundDirector.OnTntLit(MouseOverObject.IntersectedBlockPos);
                }
            }
            else if (hitBlock.IsOverridable &&
                     selected.CanAddBlockAt(world, MouseOverObject.IntersectedBlockPos))
            {
                // The block being looked at can be replaced outright, so the new block takes its place.
                _game.Client.WritePacket(new PlaceBlockPacket(
                    BuildStateToPlaceAt(MouseOverObject.IntersectedBlockPos),
                    MouseOverObject.IntersectedBlockPos));
            }
            else if (selected.CanAddBlockAt(world, MouseOverObject.BlockPlacePosition))
            {
                _game.Client.WritePacket(new PlaceBlockPacket(
                    BuildStateToPlaceAt(MouseOverObject.BlockPlacePosition),
                    MouseOverObject.BlockPlacePosition));
            }
        }

        if (Game.Input.OnMousePress(MouseButton.Middle))
        {
            BlockState picked = world.GetBlockAt(MouseOverObject.IntersectedBlockPos);
            if (picked.GetBlock() != BlockRegistry.Air)
            {
                SelectedBlock = picked;
            }
        }

        if (Game.Input.OnMousePress(MouseButton.Left))
        {
            OnSwingHandler?.Invoke();
            _game.Client.WritePacket(new RemoveBlockPacket(MouseOverObject.IntersectedBlockPos));
        }
    }

    /// <summary>
    /// The state to send for a placement. Fresh rather than the held one, since two blocks placed from the
    /// same selection must not end up sharing a state, and a block that cares which way it was put down —
    /// a torch against a wall — is told here, where the face that was clicked is still known.
    /// </summary>
    private BlockState BuildStateToPlaceAt(Vector3i blockPos)
    {
        BlockState state = BlockRegistry.GetState(SelectedBlock.GetBlock());

        if (state is IOrientedBlockState oriented && MouseOverObject is not null)
        {
            oriented.OrientTowardsSupport(MouseOverObject.IntersectedBlockPos - blockPos);
        }

        return state;
    }

    /// <summary>
    /// Picks what to build with. The number keys reach straight for one of the palette, and the wheel steps
    /// through it, which is what a hand on the mouse wants. What is currently held is not shown on a bar of
    /// its own: it is in the player's hand, drawn in front of them.
    /// </summary>
    private void UpdateBlockSelectionInput()
    {
        if (!_game.Window.IsFocused)
        {
            return;
        }

        for (int slot = 0; slot < BlockPalette.Blocks.Count; slot++)
        {
            if (Game.Input.OnKeyPress(Keys.D1 + slot))
            {
                SelectedBlock = BlockRegistry.GetState(BlockPalette.Blocks[slot]);
                return;
            }
        }

        float scroll = Game.Input.ScrollDelta.Y;
        if (scroll == 0)
        {
            return;
        }

        // Stepped from where the palette currently sits, or from its start when what is held was picked off
        // the world with the middle button and is not in the palette at all.
        int current = BlockPalette.IndexOf(SelectedBlock.GetBlock());
        int count = BlockPalette.Blocks.Count;
        int next = current < 0
            ? (scroll > 0 ? 0 : count - 1)
            : ((current - Math.Sign(scroll)) % count + count) % count;

        SelectedBlock = BlockRegistry.GetState(BlockPalette.Blocks[next]);
    }

    private void UpdateKeyboardMovementInput()
    {
        float speedMultiplier = Constants.PLAYER_BASE_MOVE_SPEED;

        bool focused = _game.Window.IsFocused;
        bool inputToRun = focused && (Game.Input.OnKeyDown(Keys.LeftControl) || Game.Input.OnKeyDown(Keys.RightControl));
        bool inputToCrouch = focused && (Game.Input.OnKeyDown(Keys.LeftShift) || Game.Input.OnKeyDown(Keys.RightShift));
        bool inputToMoveLeft = focused && Game.Input.OnKeyDown(Keys.A);
        bool inputToMoveBack = focused && Game.Input.OnKeyDown(Keys.S);
        bool inputToMoveRight = focused && Game.Input.OnKeyDown(Keys.D);
        bool inputToMoveForward = focused && Game.Input.OnKeyDown(Keys.W);
        bool inputToJump = focused && Game.Input.OnKeyDown(Keys.Space);
        bool inputToFly = focused && Game.Input.OnKeyPress(Keys.Space);

        // Crouching takes priority over running.
        if (inputToCrouch)
        {
            TryStartCrouching();
        }
        else
        {
            TryStopCrouching();

            if (inputToRun)
            {
                TryStartRunning();
            }
        }

        if (!inputToMoveForward || inputToMoveBack)
        {
            TryStopRunning();
        }

        if (_isInAir && !_isFlying)
        {
            speedMultiplier *= Constants.PLAYER_IN_AIR_SLOWDOWN;
        }

        // Water is heavy going, but not for someone flying through it, who is not swimming.
        if (_isInLiquid && !_isFlying)
        {
            speedMultiplier *= Constants.WATER_MOVE_MULTIPLIER;
        }

        if (_isFlying)
        {
            speedMultiplier *= Constants.PLAYER_FLYING_MULTIPLIER;
        }

        if (_isRunning)
        {
            speedMultiplier *= Constants.PLAYER_SPRINT_MULTIPLIER;
        }
        else if (_isCrouching)
        {
            if (_isFlying)
            {
                MoveVertically(-speedMultiplier);
            }
            else
            {
                speedMultiplier *= Constants.PLAYER_CROUCH_MULTIPLIER;
            }
        }

        if (inputToJump)
        {
            if (_isFlying)
            {
                MoveVertically(speedMultiplier);
            }
            else
            {
                AttemptToJump();
            }
        }

        if (inputToFly)
        {
            TryToggleFlying();
        }

        if (inputToMoveForward)
        {
            MoveHorizontally(0, speedMultiplier);
        }

        if (inputToMoveBack)
        {
            MoveHorizontally(0, -speedMultiplier);
        }

        if (inputToMoveRight)
        {
            MoveHorizontally(-speedMultiplier, 0);
        }

        if (inputToMoveLeft)
        {
            MoveHorizontally(speedMultiplier, 0);
        }
    }
}

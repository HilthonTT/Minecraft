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

    private readonly Game _game;

    private BlockState _selectedBlock = BlockRegistry.GetState(BlockRegistry.Tnt);
    private float _elapsedSecondsSinceLastPositionUpdate;

    public Camera Camera { get; }

    /// <summary>The block the player is currently looking at, or null when out of reach.</summary>
    public RayTraceResult? MouseOverObject { get; private set; }

    public ClientPlayer(Game game) : base(-1, string.Empty, null, new Vector3(-1, -1, -1))
    {
        _game = game;

        Camera = new Camera(new ProjectionMatrixInfo
        {
            DistanceNearPlane = 0.1F,
            DistanceFarPlane = 1000F,
            FieldOfView = 1.5F,
            WindowPixelWidth = game.Window.ClientSize.X,
            WindowPixelHeight = game.Window.ClientSize.Y,
        });

        OnToggleRunningHandler += OnRunningToggle;
        OnToggleCrouchingHandler += OnCrouchingToggle;
    }

    private void OnRunningToggle(bool isRunning)
    {
        if (isRunning)
        {
            Camera.SetFieldOfView(1.65F);
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
            Camera.SetFieldOfView(1.45F);
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

        if (!_game.MasterRenderer.IngameCanvas.IsTyping)
        {
            UpdateKeyboardMovementInput();
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
        if (MouseOverObject is null || !_game.Window.IsFocused)
        {
            return;
        }

        if (Game.Input.OnMousePress(MouseButton.Right))
        {
            Block hitBlock = world.GetBlockAt(MouseOverObject.IntersectedBlockPos).GetBlock();

            if (!_isCrouching && hitBlock.IsInteractable)
            {
                _game.Client.WritePacket(new PlayerBlockInteractionPacket(MouseOverObject.IntersectedBlockPos));
            }
            else if (hitBlock.IsOverridable &&
                     _selectedBlock.GetBlock().CanAddBlockAt(world, MouseOverObject.IntersectedBlockPos))
            {
                // The block being looked at can be replaced outright, so the new block takes its place.
                BlockState newBlock = BlockRegistry.GetState(_selectedBlock.GetBlock());
                _game.Client.WritePacket(new PlaceBlockPacket(newBlock, MouseOverObject.IntersectedBlockPos));
            }
            else if (_selectedBlock.GetBlock().CanAddBlockAt(world, MouseOverObject.BlockPlacePosition))
            {
                BlockState newBlock = BlockRegistry.GetState(_selectedBlock.GetBlock());
                _game.Client.WritePacket(new PlaceBlockPacket(newBlock, MouseOverObject.BlockPlacePosition));
            }
        }

        if (Game.Input.OnMousePress(MouseButton.Middle))
        {
            _selectedBlock = world.GetBlockAt(MouseOverObject.IntersectedBlockPos);
        }

        if (Game.Input.OnMousePress(MouseButton.Left))
        {
            _game.Client.WritePacket(new RemoveBlockPacket(MouseOverObject.IntersectedBlockPos));
        }
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

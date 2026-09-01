using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Physics;
using Minecraft.Core.Render;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Entities.Player;

public sealed class ClientPlayer : Player
{
    private const float SecondsPerPositionUpdate = 0.1F;

    private const float MaxAttackReach = 3.0F;

    private readonly Game _game;

    private float _elapsedSecondsSinceLastPositionUpdate;

    private float _highestPointOfFall;
    private bool _wasInAir;

    private readonly PlayerCameraRig _cameraRig;
    private readonly BlockBreaker _blockBreaker;

    public Camera Camera => _cameraRig.Camera;

    public int Health { get; private set; } = Constants.PLAYER_MAX_HEALTH;

    public float BreakProgress => _blockBreaker.Progress;

    public Vector3 EyePosition => _cameraRig.EyePosition;

    public CameraPerspective Perspective => _cameraRig.Perspective;

    public bool IsBodyVisible => _cameraRig.IsBodyVisible;

    public Inventory Inventory { get; } = new();

    private ushort _reportedHeldItemId;
    private int _reportedHeldDamage;

    public BlockState SelectedBlock { get; private set; } = BlockRegistry.GetState(BlockRegistry.Air);

    public event Action? OnSwingHandler;

    public RayTraceResult? MouseOverObject { get; private set; }

    public Mob? MouseOverEntity { get; private set; }

    public ClientPlayer(Game game) : base(-1, string.Empty, null, new Vector3(-1, -1, -1))
    {
        _game = game;

        _cameraRig = new PlayerCameraRig(new ProjectionMatrixInfo
        {
            DistanceNearPlane = 0.1F,
            DistanceFarPlane = 1000F,
            FieldOfView = game.Settings.FieldOfViewRadians,
            WindowPixelWidth = game.Window.ClientSize.X,
            WindowPixelHeight = game.Window.ClientSize.Y,
        });

        _blockBreaker = new BlockBreaker(game, this, () => OnSwingHandler?.Invoke());

        OnToggleRunningHandler += _cameraRig.OnRunningToggle;
        OnToggleCrouchingHandler += _cameraRig.OnCrouchingToggle;

        Inventory.OnChangedHandler += OnInventoryChanged;
        OnInventoryChanged();
    }

    private void OnInventoryChanged()
    {
        Block block = Inventory.Selected.Block ?? BlockRegistry.Air;

        if (SelectedBlock.GetBlock() != block)
        {
            SelectedBlock = BlockRegistry.GetState(block);
        }

        SendHeldItemIfChanged();
    }

    public void ReportHeldItem()
    {
        _reportedHeldItemId = 0;
        _reportedHeldDamage = 0;
        SendHeldItemIfChanged();
    }

    private void SendHeldItemIfChanged()
    {
        if (_game.Client is null)
        {
            return;
        }

        ItemStack selected = Inventory.Selected;

        ushort itemId = selected.IsEmpty ? (ushort)0 : selected.Item!.Id;

        if (itemId == _reportedHeldItemId && selected.Damage == _reportedHeldDamage)
        {
            return;
        }

        _reportedHeldItemId = itemId;
        _reportedHeldDamage = selected.Damage;

        _game.Client.WritePacket(new PlayerHeldItemPacket(itemId, selected.Damage));
    }

    public void ApplyFieldOfViewSetting()
    {
        _cameraRig.SetDefaultFieldOfView(_game.Settings.FieldOfViewRadians);
    }

    public override void SetGameMode(GameMode gameMode)
    {
        base.SetGameMode(gameMode);
        Inventory.ApplyGameMode(gameMode);
        _blockBreaker.Stop();
    }

    public void SetHealth(int health) => Health = Math.Clamp(health, 0, Constants.PLAYER_MAX_HEALTH);

    public void RespawnAt(Vector3 spawnPosition)
    {
        Position = spawnPosition;
        Velocity = Vector3.Zero;
        Acceleration = Vector3.Zero;

        ResetMovementState();
        ResetFallTracking();
        _blockBreaker.Stop();

        _cameraRig.UpdatePosition(_game.World, Position);

        _game.Client.WritePacket(new EntityDataPacket(ID, Position, Velocity, Yaw));
    }

    public void ResetForNewSession()
    {
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
        MouseOverEntity = null;
        _elapsedSecondsSinceLastPositionUpdate = 0;

        Health = Constants.PLAYER_MAX_HEALTH;

        _cameraRig.Reset();

        _reportedHeldItemId = 0;
        _reportedHeldDamage = 0;

        Inventory.ResetToDefaults();

        ResetMovementState();
        ResetFallTracking();
        _blockBreaker.Stop();
        ForgetCurrentChunk();
        UpdateAxisAlignedBox();
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
            UpdatePerspectiveInput();
            UpdateDropInput();
        }

        ApplyVelocityAndCheckCollision(deltaTime, world);

        UpdateFallTracking();

        _cameraRig.UpdatePosition(world, Position);

        MouseOverObject = new Ray(EyePosition, Camera.LookDirection).TraceWorld(world, MaxBlockReach);
        MouseOverEntity = FindMobUnderCrosshair(world);

        UpdateMouseInput(world);
        _blockBreaker.Update(deltaTime, world, FindBlockBeingDug());

        _realForward = Camera.LookDirection;
        Yaw = Camera.Yaw;
        UpdateMovementBasisFromYaw();

        _elapsedSecondsSinceLastPositionUpdate += deltaTime;
        if (_elapsedSecondsSinceLastPositionUpdate > SecondsPerPositionUpdate)
        {
            _elapsedSecondsSinceLastPositionUpdate = 0;
            _game.Client.WritePacket(new EntityDataPacket(ID, Position, Velocity, Yaw));
        }
    }

    private Vector3i? FindBlockBeingDug()
    {
        bool digging = _game.Window.IsFocused &&
                       _game.IsGameplayInputEnabled &&
                       Game.Input.OnMouseDown(MouseButton.Left) &&
                       MouseOverEntity is null &&
                       MouseOverObject is not null;

        return digging ? MouseOverObject!.IntersectedBlockPos : null;
    }

    private Mob? FindMobUnderCrosshair(World world)
    {
        var ray = new Ray(EyePosition, Camera.LookDirection);

        float nearest = MouseOverObject is null
            ? MaxAttackReach
            : MathF.Min(MaxAttackReach, (MouseOverObject.IntersectionPoint - EyePosition).Length);

        Mob? nearestMob = null;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is not Mob mob)
            {
                continue;
            }

            float distance = mob.Hitbox.Intersects(ray);
            if (distance < nearest)
            {
                nearest = distance;
                nearestMob = mob;
            }
        }

        return nearestMob;
    }

    private void UpdateMouseInput(World world)
    {
        if (!_game.Window.IsFocused || !_game.IsGameplayInputEnabled)
        {
            return;
        }

        if (Game.Input.OnMousePress(MouseButton.Left))
        {
            OnSwingHandler?.Invoke();

            if (MouseOverEntity is not null)
            {
                _game.Client.WritePacket(new PlayerAttackEntityPacket(MouseOverEntity.ID));

                if (Inventory.WearSelected())
                {
                    _game.SoundDirector.OnToolBroke(Position);
                }
            }
        }

        if (MouseOverObject is null)
        {
            return;
        }

        if (Game.Input.OnMousePress(MouseButton.Right))
        {
            Block hitBlock = world.GetBlockAt(MouseOverObject.IntersectedBlockPos).GetBlock();
            Block selected = SelectedBlock.GetBlock();

            OnSwingHandler?.Invoke();

            if (!_isCrouching && hitBlock == BlockRegistry.CraftingTable)
            {
                _game.OpenCraftingTable();
            }
            else if (!_isCrouching && hitBlock.IsInteractable)
            {
                _game.Client.WritePacket(new PlayerBlockInteractionPacket(MouseOverObject.IntersectedBlockPos));

                if (hitBlock == BlockRegistry.Tnt)
                {
                    _game.SoundDirector.OnTntLit(MouseOverObject.IntersectedBlockPos);
                }
            }
            else if (selected == BlockRegistry.Air)
            {
            }
            else if (hitBlock.IsOverridable &&
                     selected.CanAddBlockAt(world, MouseOverObject.IntersectedBlockPos))
            {
                TryPlaceAt(world, MouseOverObject.IntersectedBlockPos);
            }
            else if (selected.CanAddBlockAt(world, MouseOverObject.BlockPlacePosition))
            {
                TryPlaceAt(world, MouseOverObject.BlockPlacePosition);
            }
        }

        if (Game.Input.OnMousePress(MouseButton.Middle))
        {
            Block picked = world.GetBlockAt(MouseOverObject.IntersectedBlockPos).GetBlock();
            if (picked != BlockRegistry.Air)
            {
                Inventory.PickBlock(picked);
            }
        }
    }

    private void TryPlaceAt(World world, Vector3i blockPos)
    {
        BlockState state = BuildStateToPlaceAt(blockPos);

        if (world.IsBlockedByEntity(blockPos, state))
        {
            return;
        }

        if (!Inventory.TryConsumeSelected())
        {
            return;
        }

        _game.Client.WritePacket(new PlaceBlockPacket(state, blockPos));
    }

    private BlockState BuildStateToPlaceAt(Vector3i blockPos)
    {
        BlockState state = BlockRegistry.GetState(SelectedBlock.GetBlock());

        if (state is IOrientedBlockState oriented && MouseOverObject is not null)
        {
            oriented.OrientTowardsSupport(MouseOverObject.IntersectedBlockPos - blockPos);
        }

        return state;
    }

    private void UpdateFallTracking()
    {
        if (IsCreative || _isFlying || _isInLiquid)
        {
            ResetFallTracking();
            return;
        }

        if (_isInAir)
        {
            _highestPointOfFall = _wasInAir ? MathF.Max(_highestPointOfFall, Position.Y) : Position.Y;
            _wasInAir = true;
            return;
        }

        if (_wasInAir)
        {
            float fallen = _highestPointOfFall - Position.Y;
            if (fallen > Constants.PLAYER_SAFE_FALL_BLOCKS)
            {
                _game.Client.WritePacket(new PlayerFellPacket(fallen));
            }
        }

        ResetFallTracking();
    }

    private void ResetFallTracking()
    {
        _wasInAir = false;
        _highestPointOfFall = Position.Y;
    }

    private void UpdateBlockSelectionInput()
    {
        if (!_game.Window.IsFocused)
        {
            return;
        }

        for (int slot = 0; slot < Inventory.HotbarSlots; slot++)
        {
            if (Game.Input.OnKeyPress(Keys.D1 + slot))
            {
                Inventory.SelectHotbarSlot(slot);
                return;
            }
        }

        float scroll = Game.Input.ScrollDelta.Y;
        if (scroll != 0)
        {
            Inventory.StepHotbarSelection(-Math.Sign(scroll));
        }
    }

    private void UpdateDropInput()
    {
        if (!_game.Window.IsFocused || IsCreative || !Game.Input.OnKeyPress(Keys.Q))
        {
            return;
        }

        bool wholeStack = Game.Input.OnKeyDown(Keys.LeftControl) || Game.Input.OnKeyDown(Keys.RightControl);

        ItemStack thrown = Inventory.TakeFromSelected(wholeStack ? ItemStack.MaxCount : 1);
        if (thrown.IsEmpty)
        {
            return;
        }

        _game.Client.WritePacket(
            new PlayerDropItemPacket(thrown.Item!.Id, thrown.Count, thrown.Damage));
        OnSwingHandler?.Invoke();
    }

    private void UpdatePerspectiveInput()
    {
        if (_game.Window.IsFocused && Game.Input.OnKeyPress(Keys.F5))
        {
            _cameraRig.CyclePerspective();
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

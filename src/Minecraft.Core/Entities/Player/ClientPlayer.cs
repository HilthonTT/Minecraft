using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Physics;
using Minecraft.Core.Render;
using Minecraft.Core.Utilities.Vectors;
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

    /// <summary>
    /// How far a punch reaches. An arm's length, deliberately nothing like the reach this game gives a
    /// block: something far enough off to be a speck on the horizon is something to walk up to rather than
    /// something to hit from where you are standing.
    /// </summary>
    private const float MaxAttackReach = 3.0F;

    /// <summary>
    /// How often the arm swings while a block is being dug out, in seconds. Matched to how long one swing
    /// takes to play, so that mining reads as a run of blows rather than as one long reach.
    /// </summary>
    private const float SecondsBetweenMiningSwings = 0.3F;

    /// <summary>
    /// The shortest time that can pass between one block being taken and the next, in seconds.
    /// <para>
    /// Only a break that takes no time to begin with ever waits on this — a dig that is timed is already
    /// longer than it. Without it a block that comes apart on the frame it is aimed at, which is every block
    /// in creative, takes the one behind it as well: the removal moves the crosshair onto whatever was
    /// standing behind it, and a button still held from the same click breaks that one too.
    /// </para>
    /// </summary>
    private const float SecondsBetweenBreaks = 0.25F;

    /// <summary>How far behind — or in front of — the player the camera stands in a third person view.</summary>
    private const float ThirdPersonDistance = 4.0F;

    /// <summary>
    /// How far short of a wall the camera stops when something is in the way. Without it the eye would sit
    /// exactly on the face of the block, which is close enough for the near plane to cut into it.
    /// </summary>
    private const float ThirdPersonWallMargin = 0.25F;

    /// <summary>How finely the way back to the camera is walked when looking for something in the way.</summary>
    private const float ThirdPersonStep = 0.1F;

    private readonly Game _game;

    private float _elapsedSecondsSinceLastPositionUpdate;

    /// <summary>Which block is being dug out, or null while nothing is.</summary>
    private Vector3i? _breakingBlockPos;

    private float _secondsSpentBreaking;
    private float _secondsUntilNextMiningSwing;

    /// <summary>
    /// How long until another block may be taken. Deliberately not tied to which block is being dug, since
    /// what it exists to stop is one click running on into the block behind the one it broke.
    /// </summary>
    private float _secondsUntilNextBreak;

    /// <summary>
    /// Whether the removal for the block under the crosshair has already gone. It stays true until the
    /// crosshair finds a different cell, which is what stops one block from being asked for on every frame
    /// while the answer is still in flight.
    /// </summary>
    private bool _hasAskedToBreakTarget;

    /// <summary>
    /// The highest the player has been since they last left the ground, and whether they were airborne on
    /// the previous frame. Between them these are the whole of a fall: how far one was is the difference
    /// between the top of it and where the feet came to rest.
    /// </summary>
    private float _highestPointOfFall;
    private bool _wasInAir;

    public Camera Camera { get; }

    /// <summary>
    /// What the player has left, as the server last reported it. Read by the bar along the bottom of the
    /// screen and by nothing else: this side never works out what a blow costs, only what came of one.
    /// </summary>
    public int Health { get; private set; } = Constants.PLAYER_MAX_HEALTH;

    /// <summary>
    /// How far through breaking the block under the crosshair, from zero to one. Drawn as the outline
    /// around it brightening, and zero whenever nothing is being dug.
    /// </summary>
    public float BreakProgress { get; private set; }

    /// <summary>Where the player is looking from, which is the camera only while in first person.</summary>
    public Vector3 EyePosition { get; private set; }

    /// <summary>
    /// Which of the three views the world is being watched from. Nothing about the player changes with it:
    /// the eye stays where it is, and only where the picture is taken from moves.
    /// </summary>
    public CameraPerspective Perspective { get; private set; } = CameraPerspective.FirstPerson;

    /// <summary>Whether the player's own body should be drawn, which is whenever it is not in the way.</summary>
    public bool IsBodyVisible => Perspective != CameraPerspective.FirstPerson;

    /// <summary>What this player is carrying, and which of the nine hotbar slots is in hand.</summary>
    public Inventory Inventory { get; } = new();

    /// <summary>
    /// The block a right click would place, and what is drawn in the player's own hand. Kept as one instance
    /// that only changes when the selected slot does, rather than built on demand: the renderer decides
    /// whether its mesh is stale by comparing this against the one it last drew.
    /// </summary>
    public BlockState SelectedBlock { get; private set; } = BlockRegistry.GetState(BlockRegistry.Air);

    /// <summary>
    /// Raised when the player swings at the world — breaking, placing or interacting. Watched by the renderer
    /// that draws the held block, which is the only thing an arm swing is visible in.
    /// </summary>
    public event Action? OnSwingHandler;

    /// <summary>The block the player is currently looking at, or null when out of reach.</summary>
    public RayTraceResult? MouseOverObject { get; private set; }

    /// <summary>
    /// The mob a punch would land on, or null when there is none under the crosshair within reach. Held
    /// alongside <see cref="MouseOverObject"/> and worked out at the same moment, since which of the two a
    /// left click means is decided by which of them is nearer.
    /// </summary>
    public Mob? MouseOverEntity { get; private set; }

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

        Inventory.OnChangedHandler += OnInventoryChanged;
        OnInventoryChanged();
    }

    /// <summary>
    /// Takes whatever is now in the selected slot as the block to build with. An empty slot leaves the player
    /// holding air, which places nothing and draws nothing.
    /// </summary>
    private void OnInventoryChanged()
    {
        Block block = Inventory.Selected.Block ?? BlockRegistry.Air;

        if (SelectedBlock.GetBlock() != block)
        {
            SelectedBlock = BlockRegistry.GetState(block);
        }
    }

    /// <summary>Takes the field of view the player has chosen, for when they change it from the options.</summary>
    public void ApplyFieldOfViewSetting()
    {
        Camera.SetDefaultFieldOfView(_game.Settings.FieldOfViewRadians);
    }

    /// <summary>
    /// Takes the mode the server has put this player into. The inventory is a different thing in each of the
    /// two — a supply in one and a container in the other — so it is started over rather than carried across.
    /// </summary>
    public override void SetGameMode(GameMode gameMode)
    {
        base.SetGameMode(gameMode);
        Inventory.ApplyGameMode(gameMode);
        StopBreaking();
    }

    /// <summary>What the server says this player has left. Nothing here decides it; this only shows it.</summary>
    public void SetHealth(int health) => Health = Math.Clamp(health, 0, Constants.PLAYER_MAX_HEALTH);

    /// <summary>
    /// Puts the player back on their feet at the spawn after a death. The one time the server moves a body
    /// this side simulates, so everything the simulation was in the middle of is dropped with it — a fall
    /// that was under way most of all, or the landing would be reported and charged for twice.
    /// </summary>
    public void RespawnAt(Vector3 spawnPosition)
    {
        Position = spawnPosition;
        Velocity = Vector3.Zero;
        Acceleration = Vector3.Zero;

        ResetMovementState();
        ResetFallTracking();
        StopBreaking();

        // Moved with the body rather than left to the next frame, so the view is at the spawn on the frame
        // the death is reported. A third person camera needs the world to know what it may back away
        // through, and this runs while a packet is being handled, so it is taken from the game rather than
        // handed in.
        UpdateCameraPosition(_game.World);

        _game.Client.WritePacket(new EntityDataPacket(ID, Position, Velocity, Yaw));
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
        MouseOverEntity = null;
        _elapsedSecondsSinceLastPositionUpdate = 0;

        Health = Constants.PLAYER_MAX_HEALTH;

        // A world is always entered out of your own eyes, whichever view the last one was left in.
        Perspective = CameraPerspective.FirstPerson;
        Camera.SetViewReversed(false);

        // Nothing carried follows a player out of a world, so the next one opens on whatever its own mode
        // starts a player with rather than on what was in hand when the last was left.
        Inventory.ResetToDefaults();

        ResetMovementState();
        ResetFallTracking();
        StopBreaking();
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

    /// <summary>
    /// Steps the view through the three perspectives, the way the same key does in the game this one is
    /// modelled on: out of the player's eyes, over their shoulder, then facing them.
    /// </summary>
    private void CycleCameraPerspective()
    {
        Perspective = Perspective switch
        {
            CameraPerspective.FirstPerson => CameraPerspective.ThirdPersonBack,
            CameraPerspective.ThirdPersonBack => CameraPerspective.ThirdPersonFront,
            _ => CameraPerspective.FirstPerson,
        };
    }

    /// <summary>
    /// Puts the camera where this frame's view is taken from. The eye is worked out first and kept whatever
    /// the perspective is, since it is what the player reaches out at the world from; a third person view
    /// then backs away from it along the line of sight, stopping short of anything solid in the way.
    /// </summary>
    private void UpdateCameraPosition(World world)
    {
        Vector3 eye = Position;
        eye.X += Constants.PLAYER_WIDTH / 2.0F;
        eye.Y += Constants.PLAYER_CAMERA_HEIGHT;
        eye.Z += Constants.PLAYER_LENGTH / 2.0F;
        EyePosition = eye;

        // The front view looks back down the same line, so both third person views stand off along it and
        // only differ in which end of it the camera sits at.
        bool front = Perspective == CameraPerspective.ThirdPersonFront;
        Camera.SetViewReversed(front);

        if (Perspective == CameraPerspective.FirstPerson)
        {
            Camera.SetPosition(eye);
            return;
        }

        Vector3 away = front ? Camera.LookDirection : -Camera.LookDirection;
        Camera.SetPosition(eye + away * FindUnobstructedCameraDistance(world, eye, away));
    }

    /// <summary>
    /// How far the camera can be taken from the eye before it would end up inside something. Walked out a
    /// short step at a time rather than traced as a ray, since what matters is that the camera does not pass
    /// through a wall on the way rather than what it eventually hits.
    /// </summary>
    private static float FindUnobstructedCameraDistance(World world, Vector3 eye, Vector3 direction)
    {
        for (float distance = ThirdPersonStep; distance <= ThirdPersonDistance; distance += ThirdPersonStep)
        {
            if (!IsPositionSolid(world, eye + direction * distance))
            {
                continue;
            }

            // Back off to the last step that was clear, and a little further so the near plane clears the
            // face of the block as well.
            return Math.Max(0F, distance - ThirdPersonStep - ThirdPersonWallMargin);
        }

        return ThirdPersonDistance;
    }

    /// <summary>
    /// Whether the camera would be inside something at this point. Water and plants are not, so swimming or
    /// standing in long grass leaves the view where it is instead of dragging it onto the player's back.
    /// </summary>
    private static bool IsPositionSolid(World world, Vector3 position)
    {
        Vector3i blockPos = position.ToBlockPos();
        if (world.IsOutsideBuildHeight(blockPos.Y))
        {
            return false;
        }

        BlockState state = world.GetBlockAt(blockPos);
        return state.GetBlock().GetCollisionBox(state, blockPos).Length > 0;
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

        // Straight after the move is resolved, since what tells this a fall has ended is the feet coming to
        // rest, and the very next thing to touch the position is the camera following it.
        UpdateFallTracking();

        // Before the traces below, which are aimed from the eye this works out.
        UpdateCameraPosition(world);

        MouseOverObject = new Ray(EyePosition, Camera.LookDirection).TraceWorld(world, MaxBlockReach);
        MouseOverEntity = FindMobUnderCrosshair(world);

        UpdateMouseInput(world);
        UpdateBreaking(deltaTime, world);

        _realForward = Camera.LookDirection;
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

    /// <summary>
    /// The mob a punch would land on: the nearest one the eye line runs through, within arm's reach and in
    /// front of whatever block is being looked at, since a punch does not go through a wall.
    /// </summary>
    private Mob? FindMobUnderCrosshair(World world)
    {
        var ray = new Ray(EyePosition, Camera.LookDirection);

        // A block in the way is as far as the swing gets. Nothing being looked at leaves the whole reach.
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

            // The distance along the ray to the near face of the hitbox, or float.MaxValue for a miss.
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
        // A click while the chat or a menu is open belongs to it, not to what is being looked at.
        if (!_game.Window.IsFocused || !_game.IsGameplayInputEnabled)
        {
            return;
        }

        if (Game.Input.OnMousePress(MouseButton.Left))
        {
            OnSwingHandler?.Invoke();

            // A mob standing in front of the block is what the swing lands on, and the block behind it is
            // left alone. Nothing is applied here either way: both are requests, and the server answers.
            if (MouseOverEntity is not null)
            {
                _game.Client.WritePacket(new PlayerAttackEntityPacket(MouseOverEntity.ID));
            }
        }

        // Everything below is aimed at a block, so there is nothing to do without one.
        if (MouseOverObject is null)
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
            else if (selected == BlockRegistry.Air)
            {
                // An empty slot is an empty hand. The swing above still happens, since reaching out at a
                // block with nothing in hand is a thing somebody can do.
            }
            else if (hitBlock.IsOverridable &&
                     selected.CanAddBlockAt(world, MouseOverObject.IntersectedBlockPos))
            {
                // The block being looked at can be replaced outright, so the new block takes its place.
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

    /// <summary>
    /// Asks the server to put the held block down, and pays for it out of the stack in hand.
    /// <para>
    /// The block is spent here rather than when the placement comes back confirmed, which is the one place
    /// this side gets ahead of the server. It can afford to: everything the server would refuse a placement
    /// for is tested first — the block being able to stand there, and nothing standing where it would go —
    /// so the answer is only ever no when the world changed underneath in the tenth of a second between.
    /// Waiting instead would mean a hotbar that lags a block behind every click.
    /// </para>
    /// </summary>
    private void TryPlaceAt(World world, Vector3i blockPos)
    {
        BlockState state = BuildStateToPlaceAt(blockPos);

        // The server refuses a block placed into somebody, so asking for one would spend a block on nothing.
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
    /// Digs out whatever the crosshair is held on while the left button is down.
    /// <para>
    /// Timed here rather than on the server for the same reason a fall is: this side knows what is under the
    /// crosshair on every frame, where the server sees a look direction a tenth of a second old. What is sent
    /// is the same removal request a click has always sent — the server still decides whether the block goes
    /// — and all this decides is when to ask. In creative there is nothing to time and a block goes on the
    /// press, which is what a drawing board should feel like.
    /// </para>
    /// <para>
    /// What a break that takes no time still waits on is <see cref="SecondsBetweenBreaks"/>, so that one
    /// click takes one block rather than carrying on through everything standing behind it.
    /// </para>
    /// </summary>
    private void UpdateBreaking(float deltaTime, World world)
    {
        bool digging = _game.Window.IsFocused &&
                       _game.IsGameplayInputEnabled &&
                       Game.Input.OnMouseDown(MouseButton.Left) &&
                       MouseOverEntity is null &&
                       MouseOverObject is not null;

        if (!digging)
        {
            StopBreaking();
            return;
        }

        // Ticked down here rather than beside the progress below, since the frames spent waiting on a
        // removal to come back return early and would otherwise never count towards it.
        _secondsUntilNextBreak = MathF.Max(0F, _secondsUntilNextBreak - deltaTime);

        Vector3i target = MouseOverObject!.IntersectedBlockPos;
        Block block = world.GetBlockAt(target).GetBlock();

        // Bedrock. Nothing a player is ever given gets through it, so the swing lands and goes nowhere.
        if (!block.IsBreakable && !IsCreative)
        {
            StopBreaking();
            return;
        }

        // Looking away and back starts the block over, which is what stops somebody from chipping away at
        // half the world at once by sweeping the crosshair across it.
        if (_breakingBlockPos != target)
        {
            _breakingBlockPos = target;
            _secondsSpentBreaking = 0F;
            _secondsUntilNextMiningSwing = 0F;
            _hasAskedToBreakTarget = false;
        }

        // The removal has gone; the block is only still here because the answer has not come back yet.
        // Asking again every frame until it does would be sixty requests for one block.
        if (_hasAskedToBreakTarget)
        {
            return;
        }

        float required = IsCreative ? 0F : block.SecondsToBreak;

        _secondsSpentBreaking += deltaTime;
        BreakProgress = required <= 0F ? 1F : Math.Clamp(_secondsSpentBreaking / required, 0F, 1F);

        // Kept swinging for as long as the digging lasts, since one blow is not what breaking a block looks
        // like when it takes a couple of seconds.
        _secondsUntilNextMiningSwing -= deltaTime;
        if (_secondsUntilNextMiningSwing <= 0F)
        {
            _secondsUntilNextMiningSwing = SecondsBetweenMiningSwings;
            OnSwingHandler?.Invoke();
        }

        // Progress is left to run while the cooldown does, so a dig that takes longer than the cooldown is
        // not held up by it at all and only a block that breaks on sight ever waits.
        if (BreakProgress < 1F || _secondsUntilNextBreak > 0F)
        {
            if (required <= 0F)
            {
                // Sitting at full progress for the whole wait would light up the outline that a break
                // taking no time is meant to pass straight through without ever showing.
                BreakProgress = 0F;
            }

            return;
        }

        _game.Client.WritePacket(new RemoveBlockPacket(target));

        _secondsUntilNextBreak = SecondsBetweenBreaks;
        _hasAskedToBreakTarget = true;
        BreakProgress = 0F;
    }

    private void StopBreaking()
    {
        _breakingBlockPos = null;
        _secondsSpentBreaking = 0F;
        _secondsUntilNextMiningSwing = 0F;

        // Cleared with the rest of it, so the cooldown only ever governs a button that is still held. A
        // click of its own is somebody asking for a block, and is answered however quickly it follows.
        _secondsUntilNextBreak = 0F;
        _hasAskedToBreakTarget = false;
        BreakProgress = 0F;
    }

    /// <summary>
    /// Watches for the end of a fall and reports how long it was.
    /// <para>
    /// Only this side can: the server is sent a position every tenth of a second and could not tell a drop
    /// from a walk down a staircase without rebuilding the whole flight, while the body here has just been
    /// simulated and knows exactly where it left the ground and where it stopped. What the fall costs is
    /// still the server's to decide, the same way what a punch costs is.
    /// </para>
    /// </summary>
    private void UpdateFallTracking()
    {
        // A fall through water is broken by it, and nobody flying is falling at all.
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

    /// <summary>
    /// Picks which of the nine hotbar slots is in hand. The number keys reach straight for one and the wheel
    /// steps along the row, which is what a hand on the mouse wants. Neither changes what is in a slot: that
    /// is the inventory screen's business.
    /// </summary>
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
            // Scrolling up moves towards the first slot, which is the direction the wheel turns away from the
            // hand and the way round the game it is modelled on reads it.
            Inventory.StepHotbarSelection(-Math.Sign(scroll));
        }
    }

    /// <summary>
    /// Throws down what is in hand, on the key the game this one follows uses for it: one on its own, or the
    /// whole stack with a modifier held.
    /// <para>
    /// Survival only. A creative slot has a bottomless supply behind it, so throwing one down would be a way
    /// of printing blocks into a world other people are playing in rather than a way of getting rid of
    /// something. The server turns it down there as well; this only saves asking.
    /// </para>
    /// <para>
    /// The stack leaves the inventory here rather than when the server answers, for the same reason placing
    /// a block spends it here: the inventory is this side's, so this side is where it changes, and waiting
    /// for a round trip would leave the hotbar a beat behind the key.
    /// </para>
    /// </summary>
    private void UpdateDropInput()
    {
        if (!_game.Window.IsFocused || IsCreative || !Game.Input.OnKeyPress(Keys.Q))
        {
            return;
        }

        // The same modifier the game this follows uses for it, which is also the sprint key here — so a
        // stack thrown while running is thrown whole. That is what it does there too.
        bool wholeStack = Game.Input.OnKeyDown(Keys.LeftControl) || Game.Input.OnKeyDown(Keys.RightControl);

        ItemStack thrown = Inventory.TakeFromSelected(wholeStack ? ItemStack.MaxCount : 1);
        if (thrown.IsEmpty)
        {
            return;
        }

        _game.Client.WritePacket(new PlayerDropItemPacket(thrown.Block!.Id, thrown.Count));
        OnSwingHandler?.Invoke();
    }

    /// <summary>Steps the view on to the next perspective, on the key the game this one follows uses for it.</summary>
    private void UpdatePerspectiveInput()
    {
        if (_game.Window.IsFocused && Game.Input.OnKeyPress(Keys.F5))
        {
            CycleCameraPerspective();
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

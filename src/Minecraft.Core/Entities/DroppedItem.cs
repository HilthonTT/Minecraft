using Minecraft.Core.Entities.Player;
using Minecraft.Core.Inventories;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

/// <summary>
/// A stack lying on the ground, thrown out by a block that was broken and waiting to be walked over.
/// <para>
/// Only the server simulates one and only the server decides that somebody has picked it up; a client eases
/// it towards the last position it was told about, the same way it does a mob. What a client does not hold is
/// the inventory it lands in — that lives on the client alone — so the server takes the item out of the world
/// and tells the one player who collected it what they now have.
/// </para>
/// </summary>
public sealed class DroppedItem : Entity
{
    /// <summary>How big the thing lying on the ground is. A quarter of a block, as in the game this follows.</summary>
    public const float BodySize = 0.25F;

    /// <summary>
    /// How long before something a broken block left behind can be picked up, in seconds. Without it a block
    /// breaks and vanishes in the same frame, and what the player sees is a block disappearing rather than a
    /// block dropping something.
    /// </summary>
    public const float BrokenPickupDelaySeconds = 0.4F;

    /// <summary>
    /// The same for something a player threw down on purpose, which has to be long enough to turn round and
    /// walk away from. At the delay a broken block gets, throwing something away lands it at your feet and
    /// hands it straight back, and the key would do nothing at all.
    /// </summary>
    public const float ThrownPickupDelaySeconds = 2.0F;

    /// <summary>
    /// How long it lies there before it is cleared, in seconds. Five minutes, which is long enough to walk
    /// back for and short enough that a quarry floor does not fill up with what nobody came back for.
    /// </summary>
    private const float LifetimeSeconds = 300F;

    /// <summary>How far from the middle of a player it is close enough to be swept up.</summary>
    public const float PickupRadius = 1.35F;

    /// <summary>What is lying there. Never empty: an empty stack is not a thing to drop in the first place.</summary>
    public ItemStack Stack { get; }

    private readonly float _pickupDelaySeconds;

    private float _ageSeconds;

    /// <param name="pickupDelaySeconds">
    /// How long it has to lie there before anyone can collect it. Only the server ever reads this, so a
    /// client building one out of a spawn packet is not told which of the two delays it was given.
    /// </param>
    public DroppedItem(
        int id,
        World? world,
        Vector3 position,
        ItemStack stack,
        float pickupDelaySeconds = BrokenPickupDelaySeconds)
        : base(id, world, position, EntityType.DroppedItem)
    {
        Stack = stack;
        _pickupDelaySeconds = pickupDelaySeconds;
    }

    /// <summary>Whether enough time has gone by for somebody standing over it to pick it up.</summary>
    public bool CanBePickedUp => _ageSeconds >= _pickupDelaySeconds;

    /// <summary>Whether it has lain there long enough to be cleared away.</summary>
    public bool HasExpired => _ageSeconds >= LifetimeSeconds;

    protected override void SetInitialDimensions()
    {
        _width = BodySize;
        _height = BodySize;
        _length = BodySize;
    }

    public override void Update(float deltaTime, World world)
    {
        _ageSeconds += deltaTime;

        if (world is not WorldServer)
        {
            InterpolateTowardsServerState(deltaTime);
            base.Update(deltaTime, world);
            return;
        }

        // It steers itself nowhere; everything it does is the toss it was given wearing off against gravity
        // and friction, which is what makes a pile of drops spread out instead of stacking on one cell.
        Acceleration = Vector3.Zero;
        ApplyVelocityAndCheckCollision(deltaTime, world);
        base.Update(deltaTime, world);
    }

    /// <summary>
    /// The player standing near enough to sweep this up, or null when there is nobody. Measured from the
    /// middle of the body rather than its feet, so an item on the ground is collected by walking over it and
    /// one resting on a ledge by standing under it.
    /// </summary>
    public ServerPlayer? FindCollector(World world)
    {
        if (!CanBePickedUp)
        {
            return null;
        }

        Vector3 middle = Position + new Vector3(BodySize / 2F, BodySize / 2F, BodySize / 2F);

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is not ServerPlayer player || !player.IsAlive)
            {
                continue;
            }

            Vector3 chest = player.Position + new Vector3(0F, Constants.PLAYER_HEIGHT / 2F, 0F);
            if ((chest - middle).LengthSquared <= PickupRadius * PickupRadius)
            {
                return player;
            }
        }

        return null;
    }
}

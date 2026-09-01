using Minecraft.Core.Entities.Player;
using Minecraft.Core.Inventories;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

public sealed class DroppedItem : Entity
{
    public const float BodySize = 0.25F;

    public const float BrokenPickupDelaySeconds = 0.4F;

    public const float ThrownPickupDelaySeconds = 2.0F;

    private const float LifetimeSeconds = 300F;

    public const float PickupRadius = 1.35F;

    public ItemStack Stack { get; }

    private readonly float _pickupDelaySeconds;

    private float _ageSeconds;

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

    public bool CanBePickedUp => _ageSeconds >= _pickupDelaySeconds;

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

        Acceleration = Vector3.Zero;
        ApplyVelocityAndCheckCollision(deltaTime, world);
        base.Update(deltaTime, world);
    }

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

using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Worlds;

namespace Minecraft.Core.Network.Session;

public sealed class EntityTracker
{
    private const float SecondsPerUpdate = 0.1F;

    private readonly ServerSession _session;

    private readonly HashSet<int> _trackedEntities = [];

    private readonly HashSet<int> _entitiesInRange = [];
    private readonly List<int> _toStopTracking = [];

    private float _elapsedSecondsSinceUpdate;

    public EntityTracker(ServerSession session)
    {
        _session = session;
    }

    public void Update(float deltaTimeSeconds)
    {
        _elapsedSecondsSinceUpdate += deltaTimeSeconds;
        if (_elapsedSecondsSinceUpdate < SecondsPerUpdate)
        {
            return;
        }

        _elapsedSecondsSinceUpdate = 0;

        if (_session.State != SessionState.Accepted || _session.Player?.World is not WorldServer world)
        {
            return;
        }

        SendEntitiesInRange(world);
        StopTrackingEverythingElse();
    }

    private void SendEntitiesInRange(WorldServer world)
    {
        _entitiesInRange.Clear();

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is not (Mob or DroppedItem) || !IsInRange(entity))
            {
                continue;
            }

            _entitiesInRange.Add(entity.ID);

            if (!Send(DescribeEntity(entity)))
            {
                return;
            }
        }
    }

    private Packet DescribeEntity(Entity entity)
    {
        if (!_trackedEntities.Add(entity.ID))
        {
            return new EntityDataPacket(entity.ID, entity.Position, entity.Velocity, entity.Yaw);
        }

        if (entity is DroppedItem item)
        {
            return new ItemSpawnPacket(
                item.ID,
                item.Position,
                item.Stack.Item!.Id,
                item.Stack.Count,
                item.Stack.Damage);
        }

        return new EntitySpawnPacket(entity.EntityType, entity.ID, entity.Position, entity.Yaw);
    }

    private bool Send(Packet packet)
    {
        return _session.State != SessionState.Closed && _session.WritePacket(packet);
    }

    private void StopTrackingEverythingElse()
    {
        _toStopTracking.Clear();

        foreach (int entityId in _trackedEntities)
        {
            if (!_entitiesInRange.Contains(entityId))
            {
                _toStopTracking.Add(entityId);
            }
        }

        foreach (int entityId in _toStopTracking)
        {
            _trackedEntities.Remove(entityId);

            if (!Send(new EntityDespawnPacket(entityId)))
            {
                return;
            }
        }
    }

    private bool IsInRange(Entity entity)
    {
        return _session.IsChunkVisible(World.GetChunkPosition(entity.Position.X, entity.Position.Z));
    }
}

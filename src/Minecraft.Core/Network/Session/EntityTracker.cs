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

            if (!_trackedEntities.Add(entity.ID))
            {
                _session.WritePacket(new EntityDataPacket(entity.ID, entity.Position, entity.Velocity, entity.Yaw));
                continue;
            }

            if (entity is DroppedItem item)
            {
                _session.WritePacket(new ItemSpawnPacket(
                    item.ID,
                    item.Position,
                    item.Stack.Item!.Id,
                    item.Stack.Count,
                    item.Stack.Damage));
                continue;
            }

            _session.WritePacket(new EntitySpawnPacket(entity.EntityType, entity.ID, entity.Position, entity.Yaw));
        }
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
            _session.WritePacket(new EntityDespawnPacket(entityId));
        }
    }

    private bool IsInRange(Entity entity)
    {
        return _session.IsChunkVisible(World.GetChunkPosition(entity.Position.X, entity.Position.Z));
    }
}

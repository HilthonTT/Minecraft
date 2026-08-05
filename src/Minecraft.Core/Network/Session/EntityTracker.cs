using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Worlds;

namespace Minecraft.Core.Network.Session;

/// <summary>
/// Keeps one player's client up to date on the mobs around them: which ones it has been told about, and
/// where they have moved since. Every session has its own, the same way chunk loading does.
/// </summary>
public sealed class EntityTracker
{
    /// <summary>
    /// How often the client is brought up to date, matching the rate a client reports its own player at.
    /// </summary>
    private const float SecondsPerUpdate = 0.1F;

    private readonly ServerSession _session;

    /// <summary>Entity ids the client has been sent a spawn for and not yet a despawn.</summary>
    private readonly HashSet<int> _trackedEntities = [];

    // Reused every update so that keeping track of entities does not allocate on a timer.
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

        // Nothing is sent before the handshake finishes: a client that has not been accepted yet has no
        // world to put an entity into.
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
            if (entity is not Mob mob || !IsInRange(mob))
            {
                continue;
            }

            _entitiesInRange.Add(mob.ID);

            if (_trackedEntities.Add(mob.ID))
            {
                _session.WritePacket(new EntitySpawnPacket(mob.EntityType, mob.ID, mob.Position, mob.Yaw));
            }
            else
            {
                _session.WritePacket(new EntityDataPacket(mob.ID, mob.Position, mob.Velocity, mob.Yaw));
            }
        }
    }

    /// <summary>
    /// Anything tracked that was not in range this time has either walked off or stopped existing. The
    /// client is told to forget it either way, since from where it sits the two look the same.
    /// </summary>
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

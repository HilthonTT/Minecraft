using Minecraft.Core.Physics;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

public abstract class Entity
{
    public int ID { get; set; }

    public EntityType EntityType { get; }

    /// <summary>
    /// Null until the entity has been placed in a world. The local player is built before the world exists,
    /// and is given one once the server accepts the connection.
    /// </summary>
    public World? World { get; set; }

    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 Acceleration;

    public AxisAlignedBox Hitbox { get; protected set; }

    /// <summary>The chunk the entity is currently in, or null while that chunk is not loaded.</summary>
    public Chunk? Chunk { get; private set; }

    protected float _width, _height, _length;

    private Vector2 _previousChunkPos = new(float.MaxValue, float.MaxValue);

    public delegate void OnDespawned();
    public event OnDespawned? OnDespawnedHandler;

    public delegate void OnChunkChanged(World world, Vector2 gridPos);
    public event OnChunkChanged? OnChunkChangedHandler;

    protected Entity(int id, World? world, Vector3 position, EntityType entityType)
    {
        ID = id;
        World = world;
        Position = position;
        Velocity = Vector3.Zero;
        Acceleration = Vector3.Zero;
        EntityType = entityType;

        SetInitialDimensions();

        Vector3 max = new(position.X + _width, position.Y + _height, position.Z + _length);
        Hitbox = new AxisAlignedBox(position, max);
    }

    public void RaiseOnDespawned() => OnDespawnedHandler?.Invoke();

    protected abstract void SetInitialDimensions();

    protected void UpdateAxisAlignedBox()
    {
        Vector3 max = new(Position.X + _width, Position.Y + _height, Position.Z + _length);
        Hitbox.SetDimensions(Position, max);
    }

    /// <summary>Called as often as possible.</summary>
    public virtual void Update(float deltaTime, World world)
    {
        UpdateAxisAlignedBox();
    }

    /// <summary>Called every tick.</summary>
    public virtual void Tick(float deltaTime, World world)
    {
        Vector2 chunkPosition = Worlds.World.GetChunkPosition(Position.X, Position.Z);
        if (_previousChunkPos != chunkPosition)
        {
            Chunk = world.LoadedChunks.TryGetValue(chunkPosition, out Chunk? newChunk) ? newChunk : null;
            OnChunkChangedHandler?.Invoke(world, chunkPosition);
        }

        _previousChunkPos = chunkPosition;
    }
}

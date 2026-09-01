using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Audio;

public sealed class SoundDirector
{
    private const float StrideBlocks = 2.0F;

    private const float SwimStrideBlocks = 1.5F;

    private const float TeleportDistance = 4F;

    private const float MinSecondsBetweenCalls = 7F;
    private const float MaxSecondsBetweenCalls = 22F;

    private const int MaxBreakSoundsPerWindow = 5;
    private const float BreakSoundWindowSeconds = 0.12F;

    private const float SilenceAfterExplosionSeconds = 1.0F;

    private const float StepVolume = 0.30F;
    private const float DigVolume = 0.85F;
    private const float SplashVolume = 0.9F;
    private const float SwimVolume = 0.4F;
    private const float CallVolume = 0.75F;
    private const float ExplosionVolume = 1.0F;

    private const float HurtVolume = 1.0F;

    private const float PickupVolume = 0.35F;

    private sealed class EntitySoundState
    {
        public Vector3 LastPosition;
        public float DistanceSinceLastStep;
        public float SecondsUntilNextCall;
        public bool WasInLiquid;
        public bool HasPosition;
    }

    private readonly Game _game;
    private readonly AudioEngine _engine;
    private readonly SoundRegistry _sounds;
    private readonly Random _random = new();

    private readonly Dictionary<int, EntitySoundState> _entityStates = [];
    private readonly List<int> _goneEntities = [];

    private float _breakSoundWindowRemaining;
    private int _breakSoundsInWindow;
    private float _blockSoundsSilencedFor;

    public SoundDirector(Game game, AudioEngine engine, SoundRegistry sounds)
    {
        _game = game;
        _engine = engine;
        _sounds = sounds;
    }

    public void OnBlockPlaced(World world, Chunk chunk, Vector3i blockPos, BlockState oldState, BlockState newState)
    {
        PlayBlockSound(newState.GetBlock(), blockPos);
    }

    public void OnBlockRemoved(World world, Chunk chunk, Vector3i blockPos, BlockState oldState)
    {
        PlayBlockSound(oldState.GetBlock(), blockPos);
    }

    public void OnTntLit(Vector3i blockPos)
    {
        _engine.PlayAt(_sounds.Get(Sound.TntFuse).Pick(_random), CentreOf(blockPos));
    }

    public void OnExplosion(Vector3 position)
    {
        _blockSoundsSilencedFor = SilenceAfterExplosionSeconds;
        _engine.PlayAt(_sounds.Get(Sound.Explode).Pick(_random), position, ExplosionVolume, RandomPitch());
    }

    public void OnEntityHurt(Entity entity, bool died)
    {
        Sound? sound = died ? DeathSoundFor(entity.EntityType) : HurtSoundFor(entity.EntityType);
        if (sound is null)
        {
            return;
        }

        _engine.PlayAt(_sounds.Get(sound.Value).Pick(_random), entity.Position, HurtVolume, RandomPitch());
    }

    public void OnPlayerHurt(Vector3 position)
    {
        _engine.PlayAt(_sounds.Get(Sound.PlayerHurt).Pick(_random), position, HurtVolume, RandomPitch());
    }

    public void OnItemPickedUp(Vector3 position)
    {
        _engine.PlayAt(_sounds.Get(Sound.ItemPickup).Pick(_random), position, PickupVolume, RandomPitch());
    }

    public void OnToolBroke(Vector3 position)
    {
        _engine.PlayAt(_sounds.Get(Sound.ToolBroke).Pick(_random), position, PickupVolume, RandomPitch());
    }

    public void OnWorldUnloaded()
    {
        _entityStates.Clear();
        _blockSoundsSilencedFor = 0F;
        _engine.StopAll();
    }

    public void Update(float deltaTime, World world)
    {
        UpdateListener();
        UpdateBreakSoundBudget(deltaTime);

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is DroppedItem)
            {
                continue;
            }

            EntitySoundState state = StateOf(entity.ID);
            UpdateMovementSounds(entity, state, world);
            UpdateCallSounds(entity, state, deltaTime);
        }

        ForgetDespawnedEntities(world);
    }

    private void UpdateListener()
    {
        Camera camera = _game.MasterRenderer.GetActiveCamera();
        _engine.UpdateListener(camera.Position, camera.Right);
    }

    private void UpdateBreakSoundBudget(float deltaTime)
    {
        _blockSoundsSilencedFor -= deltaTime;
        _breakSoundWindowRemaining -= deltaTime;
        if (_breakSoundWindowRemaining <= 0F)
        {
            _breakSoundWindowRemaining = BreakSoundWindowSeconds;
            _breakSoundsInWindow = 0;
        }
    }

    private EntitySoundState StateOf(int entityId)
    {
        if (_entityStates.TryGetValue(entityId, out EntitySoundState? state))
        {
            return state;
        }

        state = new EntitySoundState
        {
            SecondsUntilNextCall = NextCallDelay(),
        };

        _entityStates.Add(entityId, state);
        return state;
    }

    private void UpdateMovementSounds(Entity entity, EntitySoundState state, World world)
    {
        Vector3 position = entity.Position;

        if (!state.HasPosition)
        {
            state.LastPosition = position;
            state.HasPosition = true;
            state.WasInLiquid = IsFootInLiquid(world, entity);
            return;
        }

        Vector3 movement = position - state.LastPosition;
        state.LastPosition = position;

        if (movement.Length > TeleportDistance)
        {
            state.DistanceSinceLastStep = 0F;
            state.WasInLiquid = IsFootInLiquid(world, entity);
            return;
        }

        bool inLiquid = IsFootInLiquid(world, entity);

        if (inLiquid && !state.WasInLiquid)
        {
            _engine.PlayAt(_sounds.Get(Sound.Splash).Pick(_random), position, SplashVolume, RandomPitch());
            state.DistanceSinceLastStep = 0F;
        }

        state.WasInLiquid = inLiquid;

        float travelled = new Vector2(movement.X, movement.Z).Length;
        state.DistanceSinceLastStep += travelled;

        if (inLiquid)
        {
            if (state.DistanceSinceLastStep >= SwimStrideBlocks)
            {
                state.DistanceSinceLastStep = 0F;
                _engine.PlayAt(_sounds.Get(Sound.Swim).Pick(_random), position, SwimVolume, RandomPitch());
            }

            return;
        }

        if (state.DistanceSinceLastStep < StrideBlocks)
        {
            return;
        }

        if (!IsStandingOnSomething(world, entity))
        {
            return;
        }

        state.DistanceSinceLastStep = 0F;
        PlayFootstep(entity, world, position);
    }

    private void UpdateCallSounds(Entity entity, EntitySoundState state, float deltaTime)
    {
        if (CallSoundFor(entity.EntityType) is not Sound call)
        {
            return;
        }

        state.SecondsUntilNextCall -= deltaTime;
        if (state.SecondsUntilNextCall > 0F)
        {
            return;
        }

        state.SecondsUntilNextCall = NextCallDelay();
        _engine.PlayAt(_sounds.Get(call).Pick(_random), entity.Position, CallVolume, RandomPitch());
    }

    private void PlayFootstep(Entity entity, World world, Vector3 position)
    {
        if (StepSoundFor(entity.EntityType) is Sound mobStep)
        {
            _engine.PlayAt(_sounds.Get(mobStep).Pick(_random), position, StepVolume, RandomPitch());
            return;
        }

        Block ground = BlockUnder(world, entity);
        if (ground == BlockRegistry.Air)
        {
            return;
        }

        _engine.PlayAt(_sounds.Step(ground.SoundMaterial).Pick(_random), position, StepVolume, RandomPitch());
    }

    private void PlayBlockSound(Block block, Vector3i blockPos)
    {
        if (block == BlockRegistry.Air || block.IsLiquid)
        {
            return;
        }

        if (_blockSoundsSilencedFor > 0F || _breakSoundsInWindow >= MaxBreakSoundsPerWindow)
        {
            return;
        }

        _breakSoundsInWindow++;
        _engine.PlayAt(_sounds.Dig(block.SoundMaterial).Pick(_random), CentreOf(blockPos), DigVolume, RandomPitch());
    }

    private bool IsStandingOnSomething(World world, Entity entity)
    {
        if (entity.ID == _game.ClientPlayer.ID)
        {
            return entity.IsOnGround;
        }

        Block below = BlockUnder(world, entity);
        return below != BlockRegistry.Air && !below.IsLiquid;
    }

    private static Block BlockUnder(World world, Entity entity)
    {
        var below = new Vector3i(
            (int)MathF.Floor(entity.Position.X + (entity.Width / 2F)),
            (int)MathF.Floor(entity.Position.Y - 0.1F),
            (int)MathF.Floor(entity.Position.Z + (entity.Length / 2F)));

        return world.IsOutsideBuildHeight(below.Y)
            ? BlockRegistry.Air
            : world.GetBlockAt(below).GetBlock();
    }

    private static bool IsFootInLiquid(World world, Entity entity)
    {
        var feet = new Vector3i(
            (int)MathF.Floor(entity.Position.X + (entity.Width / 2F)),
            (int)MathF.Floor(entity.Position.Y + 0.1F),
            (int)MathF.Floor(entity.Position.Z + (entity.Length / 2F)));

        return !world.IsOutsideBuildHeight(feet.Y) && world.GetBlockAt(feet).GetBlock().IsLiquid;
    }

    private void ForgetDespawnedEntities(World world)
    {
        foreach (int entityId in _entityStates.Keys)
        {
            if (!world.LoadedEntities.ContainsKey(entityId))
            {
                _goneEntities.Add(entityId);
            }
        }

        foreach (int entityId in _goneEntities)
        {
            _entityStates.Remove(entityId);
        }

        _goneEntities.Clear();
    }

    private float NextCallDelay()
    {
        return MinSecondsBetweenCalls + ((float)_random.NextDouble() * (MaxSecondsBetweenCalls - MinSecondsBetweenCalls));
    }

    private float RandomPitch()
    {
        return 0.9F + ((float)_random.NextDouble() * 0.2F);
    }

    private static Vector3 CentreOf(Vector3i blockPos)
    {
        return new Vector3(blockPos.X + 0.5F, blockPos.Y + 0.5F, blockPos.Z + 0.5F);
    }

    private static Sound? CallSoundFor(EntityType entityType) => entityType switch
    {
        EntityType.Sheep => Sound.SheepSay,
        EntityType.Pig => Sound.PigSay,
        EntityType.Cow => Sound.CowSay,
        EntityType.Zombie => Sound.ZombieSay,
        _ => null,
    };

    private static Sound? HurtSoundFor(EntityType entityType) => entityType switch
    {
        EntityType.Sheep => Sound.SheepSay,
        EntityType.Pig => Sound.PigSay,
        EntityType.Cow => Sound.CowHurt,
        EntityType.Zombie => Sound.ZombieHurt,
        _ => null,
    };

    private static Sound? DeathSoundFor(EntityType entityType) => entityType switch
    {
        EntityType.Sheep => Sound.SheepSay,
        EntityType.Pig => Sound.PigDeath,
        EntityType.Cow => Sound.CowHurt,
        EntityType.Zombie => Sound.ZombieDeath,
        _ => null,
    };

    private static Sound? StepSoundFor(EntityType entityType) => entityType switch
    {
        EntityType.Sheep => Sound.SheepStep,
        EntityType.Pig => Sound.PigStep,
        EntityType.Cow => Sound.CowStep,
        EntityType.Zombie => Sound.ZombieStep,
        _ => null,
    };
}

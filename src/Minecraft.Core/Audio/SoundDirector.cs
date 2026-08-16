using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Audio;

/// <summary>
/// Decides what the world sounds like: what a step is landing on, when somebody has gone into water, and how
/// often an animal is heard from.
/// <para>
/// Entirely on the client and driven by what it can already see, so none of it needs anything new from the
/// server. Footsteps come from watching things move rather than from being told about a step, which is what
/// lets another player's footfalls sound without a packet per stride.
/// </para>
/// </summary>
public sealed class SoundDirector
{
    /// <summary>How far something walks between one footfall and the next.</summary>
    private const float StrideBlocks = 2.0F;

    /// <summary>The same while swimming, where the strokes come a little closer together.</summary>
    private const float SwimStrideBlocks = 1.5F;

    /// <summary>
    /// Movement in a single frame beyond which the entity is taken to have been put somewhere rather than to
    /// have walked there. Spawning and the first update after joining both look like enormous strides
    /// otherwise, and would land a footstep the moment a world opened.
    /// </summary>
    private const float TeleportDistance = 4F;

    /// <summary>How long an animal goes between calls, drawn afresh each time so a herd does not chorus.</summary>
    private const float MinSecondsBetweenCalls = 7F;
    private const float MaxSecondsBetweenCalls = 22F;

    /// <summary>
    /// How many block sounds may play within <see cref="BreakSoundWindowSeconds"/>. A blast takes a sphere
    /// of terrain apart a block at a time and each one is broadcast, so without a ceiling a single stick of
    /// TNT would queue several hundred sounds at once and drown everything else out.
    /// </summary>
    private const int MaxBreakSoundsPerWindow = 5;
    private const float BreakSoundWindowSeconds = 0.12F;

    /// <summary>
    /// How long block sounds are held off after a blast. Everything it took apart is reported as an ordinary
    /// removal, so without this the bang is followed by the terrain it destroyed being mined a block at a
    /// time. The blast covers them anyway, which is the sound an explosion is supposed to make.
    /// </summary>
    private const float SilenceAfterExplosionSeconds = 1.0F;

    private const float StepVolume = 0.30F;
    private const float DigVolume = 0.85F;
    private const float SplashVolume = 0.9F;
    private const float SwimVolume = 0.4F;
    private const float CallVolume = 0.75F;
    private const float ExplosionVolume = 1.0F;

    /// <summary>
    /// A mob crying out at being hit, over the top of its ordinary call: it is the one sound that says
    /// whether a swing connected, so it has to carry over whatever else is going on.
    /// </summary>
    private const float HurtVolume = 1.0F;

    /// <summary>
    /// Picking something up. Quiet, because a seam of ore broken out in a run is a dozen of these within a
    /// few seconds and the point of the sound is only that it happened.
    /// </summary>
    private const float PickupVolume = 0.35F;

    /// <summary>Tracks one entity between frames, so movement can be measured without the entity holding it.</summary>
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

    /// <summary>Wired up by the client world, which is the side that hears about a block after the fact.</summary>
    public void OnBlockPlaced(World world, Chunk chunk, Vector3i blockPos, BlockState oldState, BlockState newState)
    {
        PlayBlockSound(newState.GetBlock(), blockPos);
    }

    public void OnBlockRemoved(World world, Chunk chunk, Vector3i blockPos, BlockState oldState)
    {
        PlayBlockSound(oldState.GetBlock(), blockPos);
    }

    /// <summary>Played where the struck block stands, at the moment the client asks for it to be lit.</summary>
    public void OnTntLit(Vector3i blockPos)
    {
        _engine.PlayAt(_sounds.Get(Sound.TntFuse).Pick(_random), CentreOf(blockPos));
    }

    /// <summary>
    /// A blast, played where it went off. Loud and unattenuated by the usual volume, since it is the one
    /// thing in the world that should carry further than everything else around it.
    /// </summary>
    public void OnExplosion(Vector3 position)
    {
        _blockSoundsSilencedFor = SilenceAfterExplosionSeconds;
        _engine.PlayAt(_sounds.Get(Sound.Explode).Pick(_random), position, ExplosionVolume, RandomPitch());
    }

    /// <summary>
    /// A mob crying out at a blow, played where it is standing. The one mob sound that is not worked out
    /// from what the client can see: nothing about a mob's appearance says it has been hit, so the server
    /// has to say so, and this is what it is saying it for.
    /// </summary>
    public void OnEntityHurt(Entity entity, bool died)
    {
        Sound? sound = died ? DeathSoundFor(entity.EntityType) : HurtSoundFor(entity.EntityType);
        if (sound is null)
        {
            return;
        }

        _engine.PlayAt(_sounds.Get(sound.Value).Pick(_random), entity.Position, HurtVolume, RandomPitch());
    }

    /// <summary>
    /// The player being hurt, which the server has to say: nothing about a body that has just been walked
    /// into by a zombie or landed hard looks any different from one that has not.
    /// </summary>
    public void OnPlayerHurt(Vector3 position)
    {
        _engine.PlayAt(_sounds.Get(Sound.PlayerHurt).Pick(_random), position, HurtVolume, RandomPitch());
    }

    /// <summary>A stack swept up off the ground, played where the player who collected it is standing.</summary>
    public void OnItemPickedUp(Vector3 position)
    {
        _engine.PlayAt(_sounds.Get(Sound.ItemPickup).Pick(_random), position, PickupVolume, RandomPitch());
    }

    /// <summary>Drops everything remembered about a world that has been left.</summary>
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
            // A stack lying on the ground has no feet and nothing to say. It is still moved about by the
            // toss that threw it out and by any water it lands in, and every one of those is a stride as
            // far as the measurements below can tell.
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

    /// <summary>
    /// Footsteps, and everything that happens on the way into and through water. All of it comes from how
    /// far the entity has moved since the last frame rather than from anything it reports about itself.
    /// </summary>
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

        // Put somewhere rather than walked there, so nothing is owed for the ground covered.
        if (movement.Length > TeleportDistance)
        {
            state.DistanceSinceLastStep = 0F;
            state.WasInLiquid = IsFootInLiquid(world, entity);
            return;
        }

        bool inLiquid = IsFootInLiquid(world, entity);

        // Going in is the one water sound that is not about moving, so it is tested before the stride is.
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
            // Held rather than cleared: somebody who ran off a ledge lands a step as soon as they touch down
            // again, instead of owing another full stride for the jump.
            return;
        }

        state.DistanceSinceLastStep = 0F;
        PlayFootstep(entity, world, position);
    }

    /// <summary>
    /// An animal being heard from, on a timer of its own. Wound down whether or not anyone is close enough
    /// to hear, so a herd walked up to does not call all at once because they all came due while out of
    /// earshot.
    /// </summary>
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
        // A mob with a voice of its own walks in it; everything else takes the sound of what it is walking on.
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
        // Water has nothing to break; it is only ever taken away by something being put where it was.
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

    /// <summary>
    /// Whether there is ground under the entity. The local player already knows, having been simulated here;
    /// anything else is only eased towards what the server last said and never reports itself as landed, so
    /// what is under its feet is read out of the world instead.
    /// </summary>
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

    /// <summary>
    /// Whether the entity is standing in water, measured at the feet rather than at the middle of the body.
    /// Wading into the shallows should sound like it, which the waist deep test the physics uses would miss.
    /// </summary>
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

    /// <summary>
    /// A little either side of the recorded pitch. Without it a run across open ground is the same handful
    /// of recordings over and over, which reads as a loop rather than as footsteps.
    /// </summary>
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

    /// <summary>
    /// What a mob of this kind sounds like when it is hit. The sheep and the pig have no cry of their own
    /// for it in the sound set and use their ordinary call, which is how the game it comes from does it.
    /// </summary>
    private static Sound? HurtSoundFor(EntityType entityType) => entityType switch
    {
        EntityType.Sheep => Sound.SheepSay,
        EntityType.Pig => Sound.PigSay,
        EntityType.Cow => Sound.CowHurt,
        EntityType.Zombie => Sound.ZombieHurt,
        _ => null,
    };

    /// <summary>The same for the blow that finishes it. A cow has no death recording either, only a cry.</summary>
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

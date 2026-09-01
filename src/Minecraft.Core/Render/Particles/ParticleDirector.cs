using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.Particles;

public sealed class ParticleDirector
{
    private const int BreakParticleCount = 12;

    private const float FlameVisibleDistance = 20F;

    private const float FlamesPerSecond = 2.4F;

    private const float DustStrideBlocks = 1.6F;

    private const float SilenceAfterExplosionSeconds = 1.2F;

    private const float TeleportDistance = 4F;

    private const uint LightScale = 4;

    private static readonly uint _flameLight = new Light(63, 46, 16, 0, 63).GetStorage();

    private readonly Game _game;
    private readonly ParticleSystem _system;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly Random _random = new();

    public int LiveParticleCount => _system.LiveCount;

    private float _blockParticlesSilencedFor;
    private float _distanceSinceLastDust;
    private Vector3 _lastPlayerPosition;
    private bool _hasLastPosition;
    private bool _wasInLiquid;

    public ParticleDirector(Game game, ParticleSystem system, BlockModelRegistry blockModelRegistry)
    {
        _game = game;
        _system = system;
        _blockModelRegistry = blockModelRegistry;
    }

    public void OnBlockRemoved(World world, Chunk chunk, Vector3i blockPos, BlockState oldState)
    {
        if (_blockParticlesSilencedFor > 0F ||
            oldState.GetBlock() == BlockRegistry.Air ||
            oldState.GetBlock().IsLiquid)
        {
            return;
        }

        if (!TryGetTexture(oldState, out Vector2 cellMin, out Vector2 cellMax))
        {
            return;
        }

        uint light = SampleLight(world, blockPos);
        Vector3 centre = new(blockPos.X + 0.5F, blockPos.Y + 0.5F, blockPos.Z + 0.5F);

        for (int i = 0; i < BreakParticleCount; i++)
        {
            (Vector2 uvMin, Vector2 uvMax) = RandomPatchOf(cellMin, cellMax, quarters: 4);

            _system.Spawn(new Particle
            {
                Position = centre + RandomOffset(0.5F),
                Velocity = RandomOffset(1F) * 2.6F + new Vector3(0, 2.4F, 0),
                RemainingSeconds = 0.5F + (_random.NextSingle() * 0.7F),
                TotalSeconds = 1.2F,
                Size = 0.10F + (_random.NextSingle() * 0.05F),
                UVMin = uvMin,
                UVMax = uvMax,
                Gravity = -14F,
                Drag = 1.4F,
                CollidesWithWorld = true,
                PackedLight = light,
            });
        }
    }

    public void OnExplosion(World world, Vector3 position)
    {
        _blockParticlesSilencedFor = SilenceAfterExplosionSeconds;

        if (!TryGetTexture(BlockRegistry.GetState(BlockRegistry.Cobblestone), out Vector2 cellMin, out Vector2 cellMax))
        {
            return;
        }

        uint smokeLight = new Light(6, 6, 6, 0, 30).GetStorage();

        for (int i = 0; i < 90; i++)
        {
            (Vector2 uvMin, Vector2 uvMax) = RandomPatchOf(cellMin, cellMax, quarters: 2);

            _system.Spawn(new Particle
            {
                Position = position + RandomOffset(1.6F),
                Velocity = RandomOffset(1F) * 5.5F + new Vector3(0, 2F, 0),
                RemainingSeconds = 0.9F + (_random.NextSingle() * 1.4F),
                TotalSeconds = 2.3F,
                Size = 0.45F + (_random.NextSingle() * 0.5F),
                UVMin = uvMin,
                UVMax = uvMax,

                Gravity = 1.6F,
                Drag = 2.2F,
                CollidesWithWorld = false,
                PackedLight = smokeLight,
            });
        }
    }

    public void OnWorldUnloaded()
    {
        _system.Clear();
        _blockParticlesSilencedFor = 0F;
        _distanceSinceLastDust = 0F;
        _hasLastPosition = false;
    }

    public void Update(float deltaTime, World world)
    {
        _blockParticlesSilencedFor -= deltaTime;

        _system.Update(deltaTime, world);

        UpdatePlayerParticles(deltaTime, world);
        UpdateTorchFlames(deltaTime, world);
    }

    private void UpdatePlayerParticles(float deltaTime, World world)
    {
        ClientPlayer player = _game.ClientPlayer;
        Vector3 position = player.Position;

        if (!_hasLastPosition)
        {
            _lastPlayerPosition = position;
            _hasLastPosition = true;
            _wasInLiquid = IsFootInLiquid(world, position);
            return;
        }

        Vector3 movement = position - _lastPlayerPosition;
        _lastPlayerPosition = position;

        if (movement.Length > TeleportDistance)
        {
            _distanceSinceLastDust = 0F;
            _wasInLiquid = IsFootInLiquid(world, position);
            return;
        }

        bool inLiquid = IsFootInLiquid(world, position);
        if (inLiquid && !_wasInLiquid)
        {
            EmitSplash(world, position);
        }

        _wasInLiquid = inLiquid;

        if (inLiquid || player.IsFlying || !player.IsOnGround || !player.IsRunning)
        {
            return;
        }

        _distanceSinceLastDust += new Vector2(movement.X, movement.Z).Length;
        if (_distanceSinceLastDust < DustStrideBlocks)
        {
            return;
        }

        _distanceSinceLastDust = 0F;
        EmitFootstepDust(world, position);
    }

    private void EmitFootstepDust(World world, Vector3 position)
    {
        var groundPos = new Vector3i(
            (int)MathF.Floor(position.X),
            (int)MathF.Floor(position.Y - 0.1F),
            (int)MathF.Floor(position.Z));

        if (world.IsOutsideBuildHeight(groundPos.Y))
        {
            return;
        }

        BlockState ground = world.GetBlockAt(groundPos);
        if (ground.GetBlock() == BlockRegistry.Air ||
            !TryGetTexture(ground, out Vector2 cellMin, out Vector2 cellMax))
        {
            return;
        }

        uint light = SampleLight(world, groundPos.Up());

        for (int i = 0; i < 5; i++)
        {
            (Vector2 uvMin, Vector2 uvMax) = RandomPatchOf(cellMin, cellMax, quarters: 4);

            _system.Spawn(new Particle
            {
                Position = new Vector3(position.X, groundPos.Y + 1.02F, position.Z) + RandomOffset(0.2F),
                Velocity = RandomOffset(1F) * 1.1F + new Vector3(0, 1.1F, 0),
                RemainingSeconds = 0.3F + (_random.NextSingle() * 0.35F),
                TotalSeconds = 0.65F,
                Size = 0.08F,
                UVMin = uvMin,
                UVMax = uvMax,
                Gravity = -9F,
                Drag = 2.5F,
                CollidesWithWorld = true,
                PackedLight = light,
            });
        }
    }

    private void EmitSplash(World world, Vector3 position)
    {
        if (!TryGetTexture(BlockRegistry.GetState(BlockRegistry.Water), out Vector2 cellMin, out Vector2 cellMax))
        {
            return;
        }

        uint light = SampleLight(world, position.ToBlockPos());

        for (int i = 0; i < 22; i++)
        {
            (Vector2 uvMin, Vector2 uvMax) = RandomPatchOf(cellMin, cellMax, quarters: 4);

            _system.Spawn(new Particle
            {
                Position = position + new Vector3(0, 0.1F, 0) + RandomOffset(0.35F),
                Velocity = RandomOffset(1F) * 2.2F + new Vector3(0, 3.6F, 0),
                RemainingSeconds = 0.4F + (_random.NextSingle() * 0.5F),
                TotalSeconds = 0.9F,
                Size = 0.09F,
                UVMin = uvMin,
                UVMax = uvMax,
                Gravity = -13F,
                Drag = 0.8F,

                CollidesWithWorld = false,
                PackedLight = light,
            });
        }
    }

    private void UpdateTorchFlames(float deltaTime, World world)
    {
        Vector3 cameraPosition = _game.MasterRenderer.GetActiveCamera().Position;
        Vector2 centreChunk = World.GetChunkPosition(cameraPosition.X, cameraPosition.Z);
        float chance = FlamesPerSecond * deltaTime;

        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                if (!world.LoadedChunks.TryGetValue(
                        new Vector2(centreChunk.X + offsetX, centreChunk.Y + offsetZ),
                        out Chunk? chunk))
                {
                    continue;
                }

                foreach (KeyValuePair<Vector3i, BlockState> lightSource in chunk.LightSourceBlocks)
                {
                    if (lightSource.Value is not BlockStateTorch torch)
                    {
                        continue;
                    }

                    Vector3 flamePosition = FlamePositionOf(lightSource.Key, torch);
                    if ((flamePosition - cameraPosition).LengthSquared > FlameVisibleDistance * FlameVisibleDistance)
                    {
                        continue;
                    }

                    if (_random.NextSingle() < chance)
                    {
                        EmitFlame(flamePosition);
                    }
                }
            }
        }
    }

    private static Vector3 FlamePositionOf(Vector3i blockPos, BlockStateTorch torch)
    {
        var tip = new Vector3(blockPos.X + 0.5F, blockPos.Y + 0.66F, blockPos.Z + 0.5F);

        if (!torch.IsOnWall)
        {
            return tip;
        }

        Vector3i towardsWall = DirectionUtil.ToUnit(torch.Attachment);
        return tip + new Vector3(towardsWall.X * 0.16F, 0.22F, towardsWall.Z * 0.16F);
    }

    private void EmitFlame(Vector3 position)
    {
        Vector2 cellSize = Vector2.One / 16F;
        Vector2 min = (BlockAtlas.Torch + new Vector2(7F / 16F, 6F / 16F)) * cellSize;
        Vector2 max = (BlockAtlas.Torch + new Vector2(9F / 16F, 8F / 16F)) * cellSize;

        _system.Spawn(new Particle
        {
            Position = position + RandomOffset(0.04F),
            Velocity = new Vector3(0, 0.35F, 0) + (RandomOffset(1F) * 0.14F),
            RemainingSeconds = 0.5F + (_random.NextSingle() * 0.4F),
            TotalSeconds = 0.9F,
            Size = 0.06F + (_random.NextSingle() * 0.03F),
            UVMin = min,
            UVMax = max,

            Gravity = 0.9F,
            Drag = 1.8F,
            CollidesWithWorld = false,
            PackedLight = _flameLight,
        });
    }

    private bool TryGetTexture(BlockState state, out Vector2 cellMin, out Vector2 cellMax)
    {
        BlockModel model = _blockModelRegistry.Models[state.GetBlock().Id];

        BlockFace[] faces = model.GetPartialVisibleFaces(state, Vector3i.Zero, Direction.Top);
        if (faces.Length == 0)
        {
            faces = model.GetAlwaysVisibleFaces(state, Vector3i.Zero);
        }

        if (faces.Length == 0 || faces[0].TextureCoords.Length == 0)
        {
            cellMin = Vector2.Zero;
            cellMax = Vector2.Zero;
            return false;
        }

        cellMin = faces[0].TextureCoords[0];
        cellMax = cellMin;

        foreach (Vector2 coordinate in faces[0].TextureCoords)
        {
            cellMin = Vector2.ComponentMin(cellMin, coordinate);
            cellMax = Vector2.ComponentMax(cellMax, coordinate);
        }

        return true;
    }

    private (Vector2 Min, Vector2 Max) RandomPatchOf(Vector2 cellMin, Vector2 cellMax, int quarters)
    {
        Vector2 size = (cellMax - cellMin) / quarters;
        var corner = new Vector2(
            cellMin.X + (_random.Next(quarters) * size.X),
            cellMin.Y + (_random.Next(quarters) * size.Y));

        return (corner, corner + size);
    }

    private Vector3 RandomOffset(float extent)
    {
        return new Vector3(
            (_random.NextSingle() - 0.5F) * 2F * extent,
            (_random.NextSingle() - 0.5F) * 2F * extent,
            (_random.NextSingle() - 0.5F) * 2F * extent);
    }

    private static uint SampleLight(World world, Vector3i blockPos)
    {
        if (!world.IsOutsideBuildHeight(blockPos.Y) &&
            world.LoadedChunks.TryGetValue(
                World.GetChunkPosition(blockPos.X, blockPos.Z),
                out Chunk? chunk))
        {
            Vector3i local = blockPos.ToChunkLocal();
            Light light = chunk.LightMap.GetLightColorAt((uint)local.X, (uint)local.Y, (uint)local.Z, LightScale);
            light.SetBrightness(Light.MaxChannelValue);
            return light.GetStorage();
        }

        return new Light(0, 0, 0, 15 * LightScale, Light.MaxChannelValue).GetStorage();
    }

    private static bool IsFootInLiquid(World world, Vector3 position)
    {
        var feet = new Vector3i(
            (int)MathF.Floor(position.X),
            (int)MathF.Floor(position.Y + 0.1F),
            (int)MathF.Floor(position.Z));

        return !world.IsOutsideBuildHeight(feet.Y) && world.GetBlockAt(feet).GetBlock().IsLiquid;
    }
}

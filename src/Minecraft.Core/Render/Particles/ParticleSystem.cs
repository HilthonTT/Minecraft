using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.Particles;

public sealed class ParticleSystem
{
    public const int Capacity = 1200;

    private const float BounceRestitution = 0.24F;

    private readonly Particle[] _particles = new Particle[Capacity];

    private int _nextSlotHint;

    public int LiveCount { get; private set; }

    public ReadOnlySpan<Particle> Particles => _particles;

    public void Spawn(in Particle particle)
    {
        for (int offset = 0; offset < Capacity; offset++)
        {
            int slot = (_nextSlotHint + offset) % Capacity;
            if (_particles[slot].IsAlive)
            {
                continue;
            }

            _particles[slot] = particle;
            _nextSlotHint = (slot + 1) % Capacity;
            return;
        }
    }

    public void Clear()
    {
        Array.Clear(_particles);
        _nextSlotHint = 0;
        LiveCount = 0;
    }

    public void Update(float deltaTime, World world)
    {
        int live = 0;

        for (int i = 0; i < _particles.Length; i++)
        {
            ref Particle particle = ref _particles[i];
            if (!particle.IsAlive)
            {
                continue;
            }

            particle.RemainingSeconds -= deltaTime;
            if (!particle.IsAlive)
            {
                continue;
            }

            particle.Velocity.Y += particle.Gravity * deltaTime;
            particle.Velocity *= Math.Clamp(1F - (particle.Drag * deltaTime), 0F, 1F);

            if (particle.CollidesWithWorld)
            {
                MoveAgainstWorld(ref particle, world, deltaTime);
            }
            else
            {
                particle.Position += particle.Velocity * deltaTime;
            }

            live++;
        }

        LiveCount = live;
    }

    private static void MoveAgainstWorld(ref Particle particle, World world, float deltaTime)
    {
        Vector3 step = particle.Velocity * deltaTime;
        Vector3 position = particle.Position;

        if (!TryStep(world, ref position, new Vector3(step.X, 0, 0)))
        {
            particle.Velocity.X *= -BounceRestitution;
        }

        if (!TryStep(world, ref position, new Vector3(0, step.Y, 0)))
        {
            particle.Velocity.Y *= -BounceRestitution;
        }

        if (!TryStep(world, ref position, new Vector3(0, 0, step.Z)))
        {
            particle.Velocity.Z *= -BounceRestitution;
        }

        particle.Position = position;
    }

    private static bool TryStep(World world, ref Vector3 position, Vector3 step)
    {
        Vector3 destination = position + step;
        if (IsSolidAt(world, destination))
        {
            return false;
        }

        position = destination;
        return true;
    }

    private static bool IsSolidAt(World world, Vector3 position)
    {
        Vector3i blockPos = position.ToBlockPos();
        if (world.IsOutsideBuildHeight(blockPos.Y))
        {
            return false;
        }

        BlockState state = world.GetBlockAt(blockPos);

        return state.GetBlock().GetCollisionBox(state, blockPos).Length > 0;
    }
}

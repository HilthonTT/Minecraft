using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.Particles;

/// <summary>
/// Holds and moves every speck in the air. Purely client side: nothing here is told to the server or read
/// back from it, since a puff of dust changes nothing about the world it was thrown up out of.
/// <para>
/// Slots are a fixed array reused in place, so a busy moment costs no allocation and a full one drops the
/// newest speck rather than growing without bound.
/// </para>
/// </summary>
public sealed class ParticleSystem
{
    /// <summary>
    /// How many specks may be in the air at once. Reached only by something like a blast, and past it a new
    /// one is dropped: what is already flying covers it.
    /// </summary>
    public const int Capacity = 1200;

    /// <summary>How much of its speed a speck keeps after bouncing off something.</summary>
    private const float BounceRestitution = 0.24F;

    private readonly Particle[] _particles = new Particle[Capacity];

    /// <summary>Where the search for a free slot starts, so filling up is not a walk from zero every time.</summary>
    private int _nextSlotHint;

    public int LiveCount { get; private set; }

    public ReadOnlySpan<Particle> Particles => _particles;

    /// <summary>Adds a speck, or does nothing when every slot is taken.</summary>
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

    /// <summary>Drops everything in the air, for a world that is being left.</summary>
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

    /// <summary>
    /// Moves a speck one axis at a time, so that one blocked direction does not stop the other two. A speck
    /// has no width, so where a body would need a swept box this only has to ask what is in the cell it is
    /// about to enter.
    /// </summary>
    private static void MoveAgainstWorld(ref Particle particle, World world, float deltaTime)
    {
        Vector3 step = particle.Velocity * deltaTime;
        Vector3 position = particle.Position;

        // Most of the speed is lost into whatever was hit, and what is left sends the speck back the way it
        // came, which is what makes a chip of stone skitter along a floor rather than stick to it.
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

    /// <summary>
    /// Takes one axis of a step, reporting whether it went through. Moving an axis at a time is what lets a
    /// speck slide along a wall it has run into rather than stopping dead against it.
    /// </summary>
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

        // Measured by whether the block has a body to stop something rather than by whether it can be seen
        // through, so a speck falls past grass and into water and is stopped by leaves.
        return state.GetBlock().GetCollisionBox(state, blockPos).Length > 0;
    }
}

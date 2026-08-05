using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>
/// A passive mob that grazes: it ambles to a nearby spot, stands about for a while, then picks another. Every
/// animal in the game does exactly that, so the behaviour lives here and each kind only says how far it will
/// go, how briskly, and how often it can be bothered to.
/// </summary>
public abstract class Animal : Mob
{
    protected Animal(int id, World? world, Vector3 position, EntityType entityType)
        : base(id, world, position, entityType)
    {
    }

    public sealed override bool IsHostile => false;

    /// <summary>How far away the animal will pick its next spot.</summary>
    protected abstract int WanderRadius { get; }

    /// <summary>Ticks between two decisions about whether to move on.</summary>
    protected abstract int TicksBetweenDecisions { get; }

    /// <summary>
    /// One decision in this many sends the animal somewhere; the rest leave it where it is. Kept well above
    /// one so a herd that appeared together does not then move as one body.
    /// </summary>
    protected abstract int OneInChanceOfMoving { get; }

    protected sealed override void DecideWhatToDo(WorldServer world)
    {
        TickWandering(WanderRadius, TicksBetweenDecisions, OneInChanceOfMoving);
    }
}

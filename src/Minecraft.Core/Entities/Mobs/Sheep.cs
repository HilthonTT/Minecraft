using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>A passive mob that ambles to a nearby spot, stands about for a while, then picks another.</summary>
public sealed class Sheep : Mob
{
    public const float BodyWidth = 0.9F;
    public const float BodyHeight = 1.3F;
    public const float BodyLength = 0.9F;

    /// <summary>How far away a sheep will pick its next spot.</summary>
    private const int WanderRadius = 8;

    /// <summary>Ticks between two decisions about whether to move on.</summary>
    private const int TicksBetweenDecisions = 40;

    /// <summary>One decision in this many sends the sheep somewhere; the rest leave it grazing.</summary>
    private const int OneInChanceOfMoving = 3;

    public Sheep(int id, World? world, Vector3 position) : base(id, world, position, EntityType.Sheep)
    {
    }

    public override bool IsHostile => false;

    protected override float MoveSpeed => 18F;

    protected override void SetInitialDimensions()
    {
        _width = BodyWidth;
        _height = BodyHeight;
        _length = BodyLength;
    }

    protected override void DecideWhatToDo(WorldServer world)
    {
        TickWandering(WanderRadius, TicksBetweenDecisions, OneInChanceOfMoving);
    }
}

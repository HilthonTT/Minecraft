using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>A flock animal that drifts across open grass, grazing far more of the time than it walks.</summary>
public sealed class Sheep : Animal
{
    public const float BodyWidth = 0.9F;
    public const float BodyHeight = 1.3F;
    public const float BodyLength = 0.9F;

    public Sheep(int id, World? world, Vector3 position) : base(id, world, position, EntityType.Sheep)
    {
    }

    protected override float MoveSpeed => 18F;

    protected override int WanderRadius => 8;

    protected override int TicksBetweenDecisions => 40;

    protected override int OneInChanceOfMoving => 3;

    protected override void SetInitialDimensions()
    {
        _width = BodyWidth;
        _height = BodyHeight;
        _length = BodyLength;
    }
}

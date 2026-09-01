using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

public sealed class Cow : Animal
{
    public const float BodyWidth = 0.9F;
    public const float BodyHeight = 1.4F;
    public const float BodyLength = 0.9F;

    public const int FullHealth = 10;

    public Cow(int id, World? world, Vector3 position)
        : base(id, world, position, EntityType.Cow, FullHealth)
    {
    }

    protected override float MoveSpeed => 16F;

    protected override int WanderRadius => 9;

    protected override int TicksBetweenDecisions => 50;

    protected override int OneInChanceOfMoving => 3;

    protected override void SetInitialDimensions()
    {
        _width = BodyWidth;
        _height = BodyHeight;
        _length = BodyLength;
    }
}

using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>
/// The busiest of the animals: it covers less ground than a sheep but makes up its mind twice as often, so a
/// drove of them is forever shuffling about rather than standing still.
/// </summary>
public sealed class Pig : Animal
{
    public const float BodyWidth = 0.9F;
    public const float BodyHeight = 0.9F;
    public const float BodyLength = 0.9F;

    public const int FullHealth = 10;

    public Pig(int id, World? world, Vector3 position)
        : base(id, world, position, EntityType.Pig, FullHealth)
    {
    }

    protected override float MoveSpeed => 20F;

    protected override int WanderRadius => 5;

    protected override int TicksBetweenDecisions => 25;

    protected override int OneInChanceOfMoving => 3;

    protected override void SetInitialDimensions()
    {
        _width = BodyWidth;
        _height = BodyHeight;
        _length = BodyLength;
    }
}

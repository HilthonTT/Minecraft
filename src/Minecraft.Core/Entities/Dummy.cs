using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

public sealed class Dummy : Entity
{
    public Dummy(int id) : base(id, world: null, new Vector3(15, 105, 15), EntityType.Dummy)
    {

    }

    protected override void SetInitialDimensions()
    {
        _width = 1;
        _height = 2;
        _length = 1;
    }
}
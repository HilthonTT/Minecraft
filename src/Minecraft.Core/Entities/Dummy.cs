using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

public sealed class Dummy : Entity
{
    public Dummy(int id) : base(id, world: null, new Vector3(15, 105, 15), EntityType.Dummy)
    {

    }

    /// <summary>Matches the model built for it in the model registry, so the hitbox lines up with what is drawn.</summary>
    protected override void SetInitialDimensions()
    {
        _width = 0.5F;
        _height = 2;
        _length = 0.5F;
    }
}
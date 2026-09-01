using Minecraft.Core.Utilities.Vectors;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Lighting;

public interface ILightSource
{
    Vector3i LightColor { get; }
}

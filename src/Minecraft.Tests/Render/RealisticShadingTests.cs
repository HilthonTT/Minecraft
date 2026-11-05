using Minecraft.Core.Render;
using Minecraft.Core.Render.MeshGenerator;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Render;

[Collection(RegistryCollection.Name)]
public sealed class RealisticShadingTests
{
    [Fact]
    public void MaterialBitsLeaveTheLightChannelsAlone()
    {
        var light = new Light(63, 63, 63, 63, 63);

        uint packed = VertexMaterials.Pack(light.GetStorage(), VertexMaterial.Water, 0F);

        Assert.Equal(light.GetStorage(), packed & ~(3U << VertexMaterials.Shift));
        Assert.Equal(VertexMaterial.Water, VertexMaterials.Unpack(packed));
    }

    [Theory]
    [InlineData(0F, VertexMaterial.Solid)]
    [InlineData(1F, VertexMaterial.Plant)]
    public void OnlyTheTopOfAPlantSways(float vertexHeight, VertexMaterial expected)
    {
        uint packed = VertexMaterials.Pack(0, VertexMaterial.Plant, vertexHeight);

        Assert.Equal(expected, VertexMaterials.Unpack(packed));
    }

    [Fact]
    public void BlocksAreGivenTheirMaterial()
    {
        Assert.Equal(VertexMaterial.Leaves, VertexMaterials.For(BlockRegistry.OakLeaves));
        Assert.Equal(VertexMaterial.Water, VertexMaterials.For(BlockRegistry.Water));
        Assert.Equal(VertexMaterial.Solid, VertexMaterials.For(BlockRegistry.Glass));
    }

    [Fact]
    public void TiltedSunStaysAUnitVectorAndKeepsItsHeightSign()
    {
        Vector3 noon = SceneLighting.TiltSunPath(Vector3.UnitY);
        Vector3 midnight = SceneLighting.TiltSunPath(-Vector3.UnitY);

        Assert.Equal(1F, noon.Length, 4);
        Assert.True(noon.Y > 0.8F);
        Assert.True(noon.Z > 0F);
        Assert.True(midnight.Y < -0.8F);
    }

    [Fact]
    public void ShadowMapCentresAheadOfTheCameraAndFacesTheLight()
    {
        Vector3 light = SceneLighting.TiltSunPath(Vector3.UnitY);
        Vector3 camera = new(100F, 70F, -40F);

        Matrix4 lightViewProjection = ShadowMap.ComputeLightViewProjection(camera, Vector3.UnitX, light);

        Vector4 projected = new Vector4(camera, 1F) * lightViewProjection;
        Assert.InRange(projected.X, -1F, 1F);
        Assert.InRange(projected.Y, -1F, 1F);
        Assert.InRange(projected.Z, -1F, 1F);

        Vector4 towardsLight = new Vector4(camera + (light * 20F), 1F) * lightViewProjection;
        Assert.True(towardsLight.Z < projected.Z);
    }
}

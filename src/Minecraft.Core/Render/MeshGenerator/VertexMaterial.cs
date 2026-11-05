using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.Types;

namespace Minecraft.Core.Render.MeshGenerator;

public enum VertexMaterial : uint
{
    Solid = 0,
    Leaves = 1,
    Plant = 2,
    Water = 3,
}

public static class VertexMaterials
{
    public const int Shift = 30;

    private const uint Mask = 0x3;

    private const float PlantSwayFloor = 0.5F;

    public static VertexMaterial For(Block block)
    {
        return block switch
        {
            BlockOakLeaves => VertexMaterial.Leaves,
            BlockGrassBlade or BlockFlower or BlockPlant or BlockSapling or BlockDeadBush or BlockWheat =>
                VertexMaterial.Plant,
            BlockWater => VertexMaterial.Water,
            _ => VertexMaterial.Solid,
        };
    }

    public static uint Pack(uint lightStorage, VertexMaterial material, float vertexHeightInBlock)
    {
        if (material == VertexMaterial.Plant && vertexHeightInBlock < PlantSwayFloor)
        {
            material = VertexMaterial.Solid;
        }

        return (lightStorage & ~(Mask << Shift)) | ((uint)material << Shift);
    }

    public static VertexMaterial Unpack(uint packed)
    {
        return (VertexMaterial)((packed >> Shift) & Mask);
    }
}

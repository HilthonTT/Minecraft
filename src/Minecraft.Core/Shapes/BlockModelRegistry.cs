using Minecraft.Core.Textures;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Shapes;

/// <summary>
/// Maps block ids onto the model used to draw them. Indexed directly by block id, so slot zero is unused.
/// </summary>
public sealed class BlockModelRegistry
{
    public BlockModel[] Models { get; }

    public BlockModelRegistry(TextureAtlas textureAtlas)
    {
        Models = new BlockModel[BlockRegistry.Count + 1];

        Models[BlockRegistry.Dirt.Id] = new BlockModelDirt(textureAtlas);
        Models[BlockRegistry.Stone.Id] = new BlockModelStone(textureAtlas);
        Models[BlockRegistry.Flower.Id] = new BlockModelFlower(textureAtlas);
        Models[BlockRegistry.Tnt.Id] = new BlockModelTnt(textureAtlas);
        Models[BlockRegistry.Grass.Id] = new BlockModelGrass(textureAtlas);
        Models[BlockRegistry.Sand.Id] = new BlockModelSand(textureAtlas);
        Models[BlockRegistry.SugarCane.Id] = new BlockModelSugarCane(textureAtlas);
        Models[BlockRegistry.Wheat.Id] = new BlockModelWheat(textureAtlas);
        Models[BlockRegistry.SandStone.Id] = new BlockModelSandstone(textureAtlas);
        Models[BlockRegistry.GrassBlade.Id] = new BlockModelGrassBlade(textureAtlas);
        Models[BlockRegistry.DeadBush.Id] = new BlockModelDeadBush(textureAtlas);
        Models[BlockRegistry.Cactus.Id] = new BlockModelCactus(textureAtlas);
        Models[BlockRegistry.OakLog.Id] = new BlockModelOakLog(textureAtlas);
        Models[BlockRegistry.OakLeaves.Id] = new BlockModelOakLeaves(textureAtlas);
        Models[BlockRegistry.Gravel.Id] = new BlockModelGravel(textureAtlas);

        // Air has no geometry, but the mesh generator still indexes the table by block id when it walks a
        // section, so the slot has to hold something that reports every side as see through.
        Models[BlockRegistry.Air.Id] = new BlockModelAir(textureAtlas);
    }
}

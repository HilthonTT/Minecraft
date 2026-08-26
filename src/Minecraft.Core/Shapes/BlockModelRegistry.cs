using Minecraft.Core.Textures;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.Types;

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
        Models[BlockRegistry.Planks.Id] = new BlockModelPlanks(textureAtlas);
        Models[BlockRegistry.Cobblestone.Id] = new BlockModelCobblestone(textureAtlas);
        Models[BlockRegistry.Bedrock.Id] = new BlockModelBedrock(textureAtlas);
        Models[BlockRegistry.CoalOre.Id] = new BlockModelCoalOre(textureAtlas);
        Models[BlockRegistry.IronOre.Id] = new BlockModelIronOre(textureAtlas);
        Models[BlockRegistry.GoldOre.Id] = new BlockModelGoldOre(textureAtlas);
        Models[BlockRegistry.RedstoneOre.Id] = new BlockModelRedstoneOre(textureAtlas);
        Models[BlockRegistry.DiamondOre.Id] = new BlockModelDiamondOre(textureAtlas);
        Models[BlockRegistry.Glowstone.Id] = new BlockModelGlowstone(textureAtlas);
        Models[BlockRegistry.MossyCobblestone.Id] = new BlockModelMossyCobblestone(textureAtlas);
        Models[BlockRegistry.Clay.Id] = new BlockModelClay(textureAtlas);
        Models[BlockRegistry.Snow.Id] = new BlockModelSnow(textureAtlas);
        Models[BlockRegistry.SnowyGrass.Id] = new BlockModelSnowyGrass(textureAtlas);
        Models[BlockRegistry.Ice.Id] = new BlockModelIce(textureAtlas);
        Models[BlockRegistry.BirchLog.Id] = new BlockModelBirchLog(textureAtlas);
        Models[BlockRegistry.SpruceLog.Id] = new BlockModelSpruceLog(textureAtlas);
        Models[BlockRegistry.Dandelion.Id] = new BlockModelDandelion(textureAtlas);
        Models[BlockRegistry.RedMushroom.Id] = new BlockModelRedMushroom(textureAtlas);
        Models[BlockRegistry.BrownMushroom.Id] = new BlockModelBrownMushroom(textureAtlas);
        Models[BlockRegistry.Torch.Id] = new TorchModel(textureAtlas);
        Models[BlockRegistry.CraftingTable.Id] = new BlockModelCraftingTable(textureAtlas);

        // Water is registered once per depth it can stand at, and every one of those is drawn the same way
        // to a different waterline, so they are filled in by walking the registry rather than by naming each
        // of the nine in turn.
        for (int id = 1; id <= BlockRegistry.Count; id++)
        {
            if (BlockRegistry.GetBlockFromIdentifier(id) is BlockWater water)
            {
                Models[id] = new BlockModelWater(textureAtlas, water.SurfaceHeight);
            }
        }

        // Air has no geometry, but the mesh generator still indexes the table by block id when it walks a
        // section, so the slot has to hold something that reports every side as see through.
        Models[BlockRegistry.Air.Id] = new BlockModelAir(textureAtlas);
    }
}

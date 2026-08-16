using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.Types;

namespace Minecraft.Core.Worlds.Blocks;

/// <summary>
/// The single source of truth for every block in the game. Ids are assigned here and must stay stable,
/// since they are what travels over the wire and what a section stores per block.
/// </summary>
public static class BlockRegistry
{
    public static readonly Block Air = new BlockAir(1);
    public static readonly Block Dirt = new BlockDirt(2);
    public static readonly Block Stone = new BlockStone(3);
    public static readonly Block Flower = new BlockFlower(4);
    public static readonly Block Tnt = new BlockTnt(5);
    public static readonly Block Grass = new BlockGrass(6);
    public static readonly Block Sand = new BlockSand(7);
    public static readonly Block SugarCane = new BlockSugarCane(8);
    public static readonly Block Wheat = new BlockWheat(9);
    public static readonly Block SandStone = new BlockSandstone(10);
    public static readonly Block GrassBlade = new BlockGrassBlade(11);
    public static readonly Block DeadBush = new BlockDeadBush(12);
    public static readonly Block Cactus = new BlockCactus(13);
    public static readonly Block OakLog = new BlockOakLog(14);
    public static readonly Block OakLeaves = new BlockOakLeaves(15);
    public static readonly Block Gravel = new BlockGravel(16);
    public static readonly Block Planks = new BlockPlanks(17);
    public static readonly Block Cobblestone = new BlockCobblestone(18);
    // The floor of the world, and the one block nothing gets through.
    public static readonly Block Bedrock = new BlockSolid(19, secondsToBreak: float.PositiveInfinity, dropsItself: false);
    public static readonly Block CoalOre = new BlockSolid(20, secondsToBreak: 2.8F);
    public static readonly Block IronOre = new BlockSolid(21, secondsToBreak: 2.8F);
    public static readonly Block GoldOre = new BlockSolid(22, secondsToBreak: 2.8F);
    public static readonly Block RedstoneOre = new BlockSolid(23, secondsToBreak: 2.8F);
    public static readonly Block DiamondOre = new BlockSolid(24, secondsToBreak: 2.8F);
    public static readonly Block Glowstone = new BlockGlowstone(25);
    public static readonly Block MossyCobblestone = new BlockSolid(26, secondsToBreak: 2.0F);
    public static readonly Block Clay = new BlockSolid(27, BlockSoundMaterial.Gravel, secondsToBreak: 0.6F);
    public static readonly Block Snow = new BlockSolid(28, BlockSoundMaterial.Snow, secondsToBreak: 0.3F);
    public static readonly Block SnowyGrass = new BlockSolid(29, BlockSoundMaterial.Grass, secondsToBreak: 0.65F);
    public static readonly Block Ice = new BlockSolid(30, secondsToBreak: 0.7F);
    public static readonly Block BirchLog = new BlockSolid(31, BlockSoundMaterial.Wood, secondsToBreak: 1.5F);
    public static readonly Block SpruceLog = new BlockSolid(32, BlockSoundMaterial.Wood, secondsToBreak: 1.5F);
    public static readonly Block Dandelion = new BlockPlant(33, () => [Dirt, Grass, SnowyGrass]);
    public static readonly Block RedMushroom = new BlockPlant(34, () => [Dirt, Grass, Stone, MossyCobblestone, Gravel]);
    public static readonly Block BrownMushroom = new BlockPlant(35, () => [Dirt, Grass, Stone, MossyCobblestone, Gravel]);
    public static readonly Block Water = new BlockWater(36, level: 0, falling: false);
    public static readonly Block Torch = new BlockTorch(37);

    // Running water, one block per depth it can stand at. How deep a cell of water is has to be readable
    // from the cell alone — the mesher, the client and the disk all only ever see a block id — and a sea is
    // far too many cells to hang a state off each of them. See BlockWater for the whole of the reasoning.
    public static readonly Block WaterFalling = new BlockWater(38, level: 0, falling: true);
    public static readonly Block WaterFlowing1 = new BlockWater(39, level: 1, falling: false);
    public static readonly Block WaterFlowing2 = new BlockWater(40, level: 2, falling: false);
    public static readonly Block WaterFlowing3 = new BlockWater(41, level: 3, falling: false);
    public static readonly Block WaterFlowing4 = new BlockWater(42, level: 4, falling: false);
    public static readonly Block WaterFlowing5 = new BlockWater(43, level: 5, falling: false);
    public static readonly Block WaterFlowing6 = new BlockWater(44, level: 6, falling: false);
    public static readonly Block WaterFlowing7 = new BlockWater(45, level: 7, falling: false);

    private static Block[] _registeredBlocks = [];
    private static BlockState[] _defaultStates = [];

    public static int Count => _registeredBlocks.Length;

    /// <summary>
    /// Returns the shared default state for a block, or a fresh one for blocks that carry per block data.
    /// Sharing is only safe for stateless blocks, since a shared mutable state would be visible at every
    /// position that block occupies.
    /// </summary>
    public static BlockState GetState(Block block)
    {
        if (block.HasCustomState)
        {
            return block.GetNewDefaultState();
        }

        return _defaultStates[block.Id - 1];
    }

    public static void RegisterBlocks()
    {
        _registeredBlocks =
        [
            Air,
            Dirt,
            Stone,
            Flower,
            Tnt,
            Grass,
            Sand,
            SugarCane,
            Wheat,
            SandStone,
            GrassBlade,
            DeadBush,
            Cactus,
            OakLog,
            OakLeaves,
            Gravel,
            Planks,
            Cobblestone,
            Bedrock,
            CoalOre,
            IronOre,
            GoldOre,
            RedstoneOre,
            DiamondOre,
            Glowstone,
            MossyCobblestone,
            Clay,
            Snow,
            SnowyGrass,
            Ice,
            BirchLog,
            SpruceLog,
            Dandelion,
            RedMushroom,
            BrownMushroom,
            Water,
            Torch,
            WaterFalling,
            WaterFlowing1,
            WaterFlowing2,
            WaterFlowing3,
            WaterFlowing4,
            WaterFlowing5,
            WaterFlowing6,
            WaterFlowing7,
        ];

        _defaultStates = new BlockState[_registeredBlocks.Length];
        for (int i = 0; i < _registeredBlocks.Length; i++)
        {
            if (_registeredBlocks[i].Id != i + 1)
            {
                throw new InvalidOperationException(
                    $"Block {_registeredBlocks[i].GetType().Name} has id {_registeredBlocks[i].Id} but is registered at slot {i + 1}.");
            }

            _defaultStates[i] = _registeredBlocks[i].GetNewDefaultState();
        }
    }

    public static Block GetBlockFromIdentifier(int id)
    {
        int arrayId = id - 1;
        if (arrayId < 0 || arrayId >= _registeredBlocks.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Invalid block id: " + id);
        }

        return _registeredBlocks[arrayId];
    }
}

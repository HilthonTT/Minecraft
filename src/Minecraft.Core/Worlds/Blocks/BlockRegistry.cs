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
    public static readonly Block Bedrock = new BlockSolid(19);
    public static readonly Block CoalOre = new BlockSolid(20);
    public static readonly Block IronOre = new BlockSolid(21);
    public static readonly Block GoldOre = new BlockSolid(22);
    public static readonly Block RedstoneOre = new BlockSolid(23);
    public static readonly Block DiamondOre = new BlockSolid(24);
    public static readonly Block Glowstone = new BlockGlowstone(25);
    public static readonly Block MossyCobblestone = new BlockSolid(26);
    public static readonly Block Clay = new BlockSolid(27);
    public static readonly Block Snow = new BlockSolid(28);
    public static readonly Block SnowyGrass = new BlockSolid(29);
    public static readonly Block Ice = new BlockSolid(30);
    public static readonly Block BirchLog = new BlockSolid(31);
    public static readonly Block SpruceLog = new BlockSolid(32);
    public static readonly Block Dandelion = new BlockPlant(33, () => [Dirt, Grass, SnowyGrass]);
    public static readonly Block RedMushroom = new BlockPlant(34, () => [Dirt, Grass, Stone, MossyCobblestone, Gravel]);
    public static readonly Block BrownMushroom = new BlockPlant(35, () => [Dirt, Grass, Stone, MossyCobblestone, Gravel]);

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

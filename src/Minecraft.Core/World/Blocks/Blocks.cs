using Minecraft.Core.World.Blocks.Types;

namespace Minecraft.Core.World.Blocks;

public static class Blocks
{
    public static int Count => _registeredBlocks.Length;

    private static Block[] _registeredBlocks = [];
    private static BlockState[] _defaultStates = [];

    public static readonly BlockAir Air = new(1);

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
        var blocks = new List<Block>
        {
            Air,
        };

        _registeredBlocks = [.. blocks];
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

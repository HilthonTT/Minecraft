using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Worlds.Chunks;

public sealed class Section
{
    private const int BlocksPerSection = 16 * 16 * 16;

    private readonly ushort[] _blocks = new ushort[BlocksPerSection];

    private readonly Dictionary<int, BlockState> _customStates = [];

    private int _numberOfOpaqueBlocks;
    private int _numberOfBlocks;

    public byte Height { get; }

    public bool IsFullTransparent { get; private set; }

    public bool IsEmpty { get; private set; }

    public int GridX { get; private set; }

    public int GridZ { get; private set; }

    public Section(int gridX, int gridZ, byte height)
    {
        GridX = gridX;
        GridZ = gridZ;
        Height = height;
        IsFullTransparent = true;
        IsEmpty = true;
    }

    public override string ToString()
    {
        return "Section[Height=" + Height +
               " FullTransparent=" + IsFullTransparent +
               " IsEmpty=" + IsEmpty +
               " NumOpaqueBlocks=" + _numberOfOpaqueBlocks +
               " NumBlocks=" + _numberOfBlocks + "]";
    }

    public void ResetAndAssign(int gridX, int gridZ)
    {
        GridX = gridX;
        GridZ = gridZ;

        if (_customStates.Count != 0)
        {
            throw new InvalidOperationException(
                "Section still held " + _customStates.Count + " custom block states after being emptied.");
        }
    }

    private static int GetIndex(int localX, int localY, int localZ)
    {
        return (localX << 8) + (localY << 4) + localZ;
    }

    private static void ValidateCoordinates(int localX, int localY, int localZ)
    {
        if (localX is < 0 or > 15 || localY is < 0 or > 15 || localZ is < 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(
                nameof(localX),
                $"({localX},{localY},{localZ}) lies outside of this section.");
        }
    }

    public void AddBlockAt(int localX, int localY, int localZ, BlockState blockState)
    {
        ValidateCoordinates(localX, localY, localZ);

        int index = GetIndex(localX, localY, localZ);
        if (_blocks[index] != 0)
        {
            RemoveBlockAt(localX, localY, localZ);
        }

        Block block = blockState.GetBlock();
        _blocks[index] = block.Id;

        if (block.HasCustomState)
        {
            _customStates[index] = blockState;
        }

        if (block.IsOpaque)
        {
            _numberOfOpaqueBlocks++;
        }

        _numberOfBlocks++;

        IsFullTransparent = _numberOfOpaqueBlocks == 0;
        IsEmpty = _numberOfBlocks == 0;
    }

    public void RemoveBlockAt(int localX, int localY, int localZ)
    {
        ValidateCoordinates(localX, localY, localZ);

        int index = GetIndex(localX, localY, localZ);
        if (_blocks[index] == 0)
        {
            return;
        }

        Block block = BlockRegistry.GetBlockFromIdentifier(_blocks[index]);
        if (block.HasCustomState && !_customStates.Remove(index))
        {
            throw new InvalidOperationException("Removing a custom state block that was not in storage.");
        }

        _blocks[index] = 0;

        if (block.IsOpaque)
        {
            _numberOfOpaqueBlocks--;
        }

        _numberOfBlocks--;

        IsFullTransparent = _numberOfOpaqueBlocks == 0;
        IsEmpty = _numberOfBlocks == 0;
    }

    public BlockState? GetBlockAt(int localX, int localY, int localZ)
    {
        int index = GetIndex(localX, localY, localZ);
        if (_blocks[index] == 0)
        {
            return null;
        }

        Block block = BlockRegistry.GetBlockFromIdentifier(_blocks[index]);
        if (!block.HasCustomState)
        {
            return BlockRegistry.GetState(block);
        }

        if (!_customStates.TryGetValue(index, out BlockState? state))
        {
            throw new InvalidOperationException("Custom state block was not in storage.");
        }

        return state;
    }
}

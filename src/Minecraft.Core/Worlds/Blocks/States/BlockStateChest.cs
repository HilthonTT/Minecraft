using Minecraft.Core.Inventories;
using Minecraft.Core.IO;
using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateChest : BlockState, IContainerState, IFacingBlockState
{
    public const int Slots = 27;

    private readonly ItemStack[] _slots = new ItemStack[Slots];

    public Direction Facing { get; set; } = Direction.Front;

    public int SlotCount => Slots;

    public override Block GetBlock() => BlockRegistry.Chest;

    public ItemStack GetSlot(int slot) => _slots[slot];

    public void SetSlot(int slot, ItemStack stack) => _slots[slot] = stack;

    public bool Accepts(int slot, ItemStack stack) => true;

    public bool IsTakeOnly(int slot) => false;

    public void ClearContents() => Array.Clear(_slots);

    public void CopyContentsFrom(BlockState source)
    {
        if (source is not BlockStateChest chest)
        {
            return;
        }

        Array.Copy(chest._slots, _slots, Slots);
    }

    public override void ToStream(BufferedDataStream bufferedStream)
    {
        base.ToStream(bufferedStream);
        bufferedStream.WriteByte((byte)Facing);

        foreach (ItemStack stack in _slots)
        {
            ItemStackCodec.Write(bufferedStream, stack);
        }
    }

    public override int PayloadSize() => sizeof(byte) + (Slots * ItemStackCodec.Size);

    public override void ExtractFromByteStream(byte[] bytes, ref int head)
    {
        Facing = FacingFrom(DataConverter.BytesToByteStruct<Direction>(bytes, ref head));

        for (int slot = 0; slot < Slots; slot++)
        {
            _slots[slot] = ItemStackCodec.Read(bytes, ref head);
        }
    }

    internal static Direction FacingFrom(Direction stored) =>
        DirectionUtil.IsHorizontal(stored) ? stored : Direction.Front;
}

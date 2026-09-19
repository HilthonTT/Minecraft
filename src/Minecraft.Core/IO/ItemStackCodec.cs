using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.IO;

public static class ItemStackCodec
{
    public const int Size = sizeof(ushort) + sizeof(int) + sizeof(int);

    public static void Write(BufferedDataStream stream, ItemStack stack)
    {
        if (stack.IsEmpty)
        {
            stream.WriteUInt16(0);
            stream.WriteInt32(0);
            stream.WriteInt32(0);
            return;
        }

        stream.WriteUInt16(stack.Item!.Id);
        stream.WriteInt32(stack.Count);
        stream.WriteInt32(stack.Damage);
    }

    public static ItemStack Read(byte[] bytes, ref int head)
    {
        ushort itemId = DataConverter.BytesToUInt16(bytes, ref head);
        int count = DataConverter.BytesToInt32(bytes, ref head);
        int damage = DataConverter.BytesToInt32(bytes, ref head);

        return FromParts(itemId, count, damage);
    }

    public static ItemStack FromParts(ushort itemId, int count, int damage)
    {
        if (itemId == 0 || itemId == BlockRegistry.Air.Id || count <= 0)
        {
            return ItemStack.Empty;
        }

        Item? item = ItemRegistry.TryGet(itemId);
        return item is null ? ItemStack.Empty : new ItemStack(item, count, damage);
    }
}

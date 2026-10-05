using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Smelting;
using Minecraft.Core.IO;
using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateFurnace : BlockState, IContainerState, IFacingBlockState
{
    public const int InputSlot = 0;
    public const int FuelSlot = 1;
    public const int OutputSlot = 2;

    public const int Slots = 3;

    private const int TicksBetweenProgressSyncs = 10;

    private readonly ItemStack[] _slots = new ItemStack[Slots];

    private int _ticksUntilProgressSync;

    public Direction Facing { get; set; } = Direction.Front;

    public int BurnTicksRemaining { get; private set; }

    public int BurnTicksTotal { get; private set; }

    public int CookTicks { get; private set; }

    public bool IsBurning => BurnTicksRemaining > 0;

    public float BurnFraction => BurnTicksTotal <= 0 ? 0F : Math.Clamp(BurnTicksRemaining / (float)BurnTicksTotal, 0F, 1F);

    public float CookFraction => Math.Clamp(CookTicks / (float)SmeltingRegistry.CookTicks, 0F, 1F);

    public ItemStack Input => _slots[InputSlot];

    public ItemStack Fuel => _slots[FuelSlot];

    public ItemStack Output => _slots[OutputSlot];

    public int SlotCount => Slots;

    public override int Appearance => IsBurning ? 1 : 0;

    public override Block GetBlock() => BlockRegistry.Furnace;

    public ItemStack GetSlot(int slot) => _slots[slot];

    public void SetSlot(int slot, ItemStack stack)
    {
        if (slot == InputSlot && stack.Item != _slots[InputSlot].Item)
        {
            CookTicks = 0;
        }

        _slots[slot] = stack;
    }

    public bool Accepts(int slot, ItemStack stack) => slot switch
    {
        InputSlot => true,
        FuelSlot => SmeltingRegistry.IsFuel(stack),
        _ => false,
    };

    public bool IsTakeOnly(int slot) => slot == OutputSlot;

    public void ClearContents()
    {
        Array.Clear(_slots);
        BurnTicksRemaining = 0;
        BurnTicksTotal = 0;
        CookTicks = 0;
    }

    public void CopyContentsFrom(BlockState source)
    {
        if (source is not BlockStateFurnace furnace)
        {
            return;
        }

        Array.Copy(furnace._slots, _slots, Slots);
        BurnTicksRemaining = furnace.BurnTicksRemaining;
        BurnTicksTotal = furnace.BurnTicksTotal;
        CookTicks = furnace.CookTicks;
    }

    public bool Step()
    {
        bool wasBurning = IsBurning;
        bool contentsChanged = false;

        if (BurnTicksRemaining > 0)
        {
            BurnTicksRemaining--;
        }

        bool canSmelt = CanSmelt();

        if (BurnTicksRemaining == 0 && canSmelt)
        {
            int burnTicks = SmeltingRegistry.BurnTicksOf(Fuel);
            if (burnTicks > 0)
            {
                BurnTicksRemaining = burnTicks;
                BurnTicksTotal = burnTicks;
                _slots[FuelSlot] = Fuel.WithCount(Fuel.Count - 1);
                contentsChanged = true;
            }
        }

        if (IsBurning && canSmelt)
        {
            CookTicks++;

            if (CookTicks >= SmeltingRegistry.CookTicks)
            {
                CookTicks = 0;
                SmeltOne();
                contentsChanged = true;
            }
        }
        else if (CookTicks > 0)
        {
            CookTicks = 0;
            contentsChanged = true;
        }

        if (!IsBurning)
        {
            BurnTicksTotal = 0;
        }

        return contentsChanged || wasBurning != IsBurning;
    }

    public bool IsProgressSyncDue()
    {
        if (!IsBurning)
        {
            _ticksUntilProgressSync = 0;
            return false;
        }

        if (_ticksUntilProgressSync > 0)
        {
            _ticksUntilProgressSync--;
            return false;
        }

        _ticksUntilProgressSync = TicksBetweenProgressSyncs;
        return true;
    }

    private bool CanSmelt()
    {
        ItemStack result = SmeltingRegistry.ResultFor(Input);
        if (result.IsEmpty)
        {
            return false;
        }

        if (Output.IsEmpty)
        {
            return true;
        }

        return Output.CanStackWith(result) && Output.Count + result.Count <= Output.MaxStackSize;
    }

    private void SmeltOne()
    {
        ItemStack result = SmeltingRegistry.ResultFor(Input);

        _slots[OutputSlot] = Output.IsEmpty ? result : Output.WithCount(Output.Count + result.Count);
        _slots[InputSlot] = Input.WithCount(Input.Count - 1);
    }

    public override void ToStream(BufferedDataStream bufferedStream)
    {
        base.ToStream(bufferedStream);
        bufferedStream.WriteByte((byte)Facing);

        foreach (ItemStack stack in _slots)
        {
            ItemStackCodec.Write(bufferedStream, stack);
        }

        bufferedStream.WriteInt32(BurnTicksRemaining);
        bufferedStream.WriteInt32(BurnTicksTotal);
        bufferedStream.WriteInt32(CookTicks);
    }

    public override int PayloadSize() => sizeof(byte) + (Slots * ItemStackCodec.Size) + (3 * sizeof(int));

    public override void ExtractFromByteStream(byte[] bytes, ref int head)
    {
        Facing = BlockStateChest.FacingFrom(DataConverter.BytesToByteStruct<Direction>(bytes, ref head));

        for (int slot = 0; slot < Slots; slot++)
        {
            _slots[slot] = ItemStackCodec.Read(bytes, ref head);
        }

        BurnTicksRemaining = Math.Max(DataConverter.BytesToInt32(bytes, ref head), 0);
        BurnTicksTotal = Math.Max(DataConverter.BytesToInt32(bytes, ref head), BurnTicksRemaining);
        CookTicks = Math.Clamp(DataConverter.BytesToInt32(bytes, ref head), 0, SmeltingRegistry.CookTicks);
    }
}

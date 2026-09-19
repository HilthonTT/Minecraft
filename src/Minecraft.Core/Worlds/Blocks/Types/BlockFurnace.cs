using Minecraft.Core.Audio;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockFurnace : Block
{
    public BlockFurnace(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Stone;
        SecondsToBreak = 3.5F;
        HarvestTool = ToolKind.Pickaxe;
        RequiresCorrectTool = true;
        IsTickable = true;
        HasCustomState = true;
    }

    public override BlockState GetNewDefaultState() => new BlockStateFurnace();

    public override void OnTick(BlockState blockState, World world, Vector3i blockPos, float deltaTime)
    {
        if (world is not WorldServer || blockState is not BlockStateFurnace furnace)
        {
            return;
        }

        bool changed = furnace.Step();
        bool progressDue = furnace.IsProgressSyncDue();

        if (changed || progressDue)
        {
            world.NotifyBlockStateChanged(blockPos, furnace);
        }
    }

    public override void OnDestroy(BlockState blockState, World world, Vector3i blockPos)
    {
        ContainerSpill.SpillContents(blockState, world, blockPos);
        base.OnDestroy(blockState, world, blockPos);
    }
}

using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks;

public abstract class Block
{
    protected readonly AxisAlignedBox[] _emptyAABB = [];
    private readonly AxisAlignedBox _fullBlockAABB = new(Vector3.Zero, Vector3.Zero);
    private readonly AxisAlignedBox[] _fullBlockAABBList = new AxisAlignedBox[1];

    public ushort Id { get; private set; }
    public bool IsTickable { get; protected set; }
    public bool IsInteractable { get; protected set; }
    public bool IsOverridable { get; protected set; }
    public bool IsOpaque { get; protected set; } = true;

    public bool IsLiquid { get; protected set; }

    public bool IsTranslucent { get; protected set; }

    /// <summary>Whether something standing in the cell can climb it, the way a ladder is climbed.</summary>
    public bool IsClimbable { get; protected set; }

    public bool HidesFacesAgainstItself { get; protected set; }

    public BlockSoundMaterial SoundMaterial { get; protected set; } = BlockSoundMaterial.Stone;
    public bool HasCustomState { get; protected set; } = false;

    public float SecondsToBreak { get; protected set; } = 1.0F;

    public bool IsBreakable => !float.IsPositiveInfinity(SecondsToBreak);

    public ToolKind? HarvestTool { get; protected set; }

    public int HarvestLevel { get; protected set; }

    public bool RequiresCorrectTool { get; protected set; }

    /// <summary>
    /// Whether the block's own item puts it down. Wheat and doors are planted and hung by something else — seeds
    /// and a door — so the item named after the block is only what they leave behind.
    /// </summary>
    public bool IsPlacedByItsOwnItem { get; protected set; } = true;

    protected Block(ushort id)
    {
        Id = id;
        _fullBlockAABBList[0] = _fullBlockAABB;
    }

    public abstract BlockState GetNewDefaultState();

    public virtual ItemStack GetDrop(BlockState blockState) => new(this, 1);

    /// <summary>
    /// Everything a player's break leaves. Most blocks leave the one stack <see cref="GetDrop"/> names; the ones
    /// that leave a chance of something extra, or something that depends on what is around them, say so here.
    /// </summary>
    public virtual void AddDrops(BlockState blockState, World world, Vector3i blockPos, Random random, List<ItemStack> drops)
    {
        ItemStack drop = GetDrop(blockState);
        if (!drop.IsEmpty)
        {
            drops.Add(drop);
        }
    }

    public virtual void OnInteract(BlockState blockstate, Vector3i blockPos, World world)
    {
    }

    public virtual bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        return true;
    }

    public virtual void OnTick(BlockState blockState, World world, Vector3i blockPos, float deltaTime)
    {
    }

    public virtual void OnScheduledUpdate(BlockState blockState, World world, Vector3i blockPos)
    {
    }

    public virtual void OnAdd(BlockState blockState, World world, Vector3i blockPos)
    {
        NotifyNeighbours(blockState, world, blockPos);
    }

    public virtual void OnDestroy(BlockState blockState, World world, Vector3i blockPos)
    {
        NotifyNeighbours(blockState, world, blockPos);
    }

    public virtual void OnNotify(BlockState blockState, BlockState sourceBlockState, World world, Vector3i blockPos, Vector3i sourceBlockPos) { }

    public virtual AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos)
    {
        _fullBlockAABBList[0] = GetFullBlockCollision(blockPos);
        return _fullBlockAABBList;
    }

    public virtual AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        _fullBlockAABBList[0] = GetFullBlockCollision(blockPos);
        return _fullBlockAABBList;
    }

    public AxisAlignedBox GetFullBlockCollision(Vector3i blockPos)
    {
        _fullBlockAABB.SetDimensions(new Vector3(blockPos.X, blockPos.Y, blockPos.Z),
            new Vector3(blockPos.X + Constants.CUBE_DIM, blockPos.Y + Constants.CUBE_DIM, blockPos.Z + Constants.CUBE_DIM));
        return _fullBlockAABB;
    }

    protected void NotifyNeighbours(BlockState blockState, World world, Vector3i blockPos)
    {
        foreach (Vector3i neighbourPos in blockPos.GetSurroundingPositions())
        {
            BlockState? state = world.GetBlockAt(neighbourPos);
            state?.GetBlock().OnNotify(state, blockState, world, neighbourPos, blockPos);
        }
    }
}

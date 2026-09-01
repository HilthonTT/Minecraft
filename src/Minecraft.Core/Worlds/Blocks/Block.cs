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

    public BlockSoundMaterial SoundMaterial { get; protected set; } = BlockSoundMaterial.Stone;
    public bool HasCustomState { get; protected set; } = false;

    public float SecondsToBreak { get; protected set; } = 1.0F;

    public bool IsBreakable => !float.IsPositiveInfinity(SecondsToBreak);

    public ToolKind? HarvestTool { get; protected set; }

    public int HarvestLevel { get; protected set; }

    public bool RequiresCorrectTool { get; protected set; }

    protected Block(ushort id)
    {
        Id = id;
        _fullBlockAABBList[0] = _fullBlockAABB;
    }

    public abstract BlockState GetNewDefaultState();

    public virtual ItemStack GetDrop(BlockState blockState) => new(this, 1);

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

using Minecraft.Core.Audio;
using Minecraft.Core.Physics;
using OpenTK.Mathematics;
using Minecraft.Core.Utilities.Vectors;

namespace Minecraft.Core.Worlds.Blocks;

public abstract class Block
{
    protected readonly AxisAlignedBox[] _emptyAABB = [];
    private readonly AxisAlignedBox _fullBlockAABB = new(Vector3.Zero, Vector3.Zero);
    private readonly AxisAlignedBox[] _fullBlockAABBList = new AxisAlignedBox[1];

    public ushort Id { get; private set; }
    //If this block has tick functionality
    public bool IsTickable { get; protected set; }
    //If this block can be interacted with
    public bool IsInteractable { get; protected set; }
    //If another block can be placed at this blocks position
    public bool IsOverridable { get; protected set; }
    //If this blocks lets light through or not
    public bool IsOpaque { get; protected set; } = true;

    /// <summary>
    /// Whether this block is a body of fluid. Liquids are drawn in a pass of their own so they can be seen
    /// through, two cells of the same liquid share no face between them, and an entity moving through one
    /// swims rather than falls.
    /// </summary>
    public bool IsLiquid { get; protected set; }

    /// <summary>
    /// Which set of sounds this block is walked on and broken with. Stone by default, which is what most of
    /// the world is made of.
    /// </summary>
    public BlockSoundMaterial SoundMaterial { get; protected set; } = BlockSoundMaterial.Stone;
    //If this block has blockstate specific data
    public bool HasCustomState { get; protected set; } = false;

    /// <summary>
    /// How long a bare hand takes to break this block, in seconds. Zero is instant, which is what anything
    /// with no body of its own comes to, and <see cref="float.PositiveInfinity"/> is bedrock: nothing a
    /// player is ever given will get through it.
    /// <para>
    /// Held as a time rather than as Minecraft's own hardness figure because there is nothing to hold yet
    /// that digs faster. When there is, this becomes the numerator and the tool the denominator; until then
    /// the two would be the same number written twice, and one of them would be wrong.
    /// </para>
    /// </summary>
    public float SecondsToBreak { get; protected set; } = 1.0F;

    /// <summary>Whether any amount of digging gets through this block.</summary>
    public bool IsBreakable => !float.IsPositiveInfinity(SecondsToBreak);

    protected Block(ushort id)
    {
        Id = id;
        _fullBlockAABBList[0] = _fullBlockAABB;
    }

    public abstract BlockState GetNewDefaultState();

    /// <summary>
    /// What breaking this block leaves behind, or null when it leaves nothing. Itself for almost everything;
    /// the exceptions are the blocks that come apart on the way out — stone into cobblestone, grass into the
    /// dirt under it — and the greenery, which is torn rather than harvested.
    /// <para>
    /// Only asked when a player broke the block by hand. Water washing a flower away, sand settling a cell
    /// lower and a blast taking a hillside apart all go through the same removal, and none of them should
    /// leave a pile of anything behind.
    /// </para>
    /// </summary>
    public virtual Block? GetDroppedBlock(BlockState blockState) => this;

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

    /// <summary>
    /// Called once the delay asked for through <see cref="World.ScheduleBlockUpdate"/> has run out. This is
    /// where a block that moves of its own accord does its work rather than in <see cref="OnTick"/>: water
    /// and sand only have something to do in the moments after something around them changed, and a block
    /// that asks to be looked at then costs nothing for all the ticks it spends at rest.
    /// </summary>
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

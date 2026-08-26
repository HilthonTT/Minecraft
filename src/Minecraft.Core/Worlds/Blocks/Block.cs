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
    /// Held as a time rather than as Minecraft's own hardness figure because it is the numerator of the one
    /// division that matters: a block's time divided by the dig speed of the right tool for it. See
    /// <see cref="Harvesting.SecondsToBreak"/>, where the two are put together.
    /// </para>
    /// </summary>
    public float SecondsToBreak { get; protected set; } = 1.0F;

    /// <summary>Whether any amount of digging gets through this block.</summary>
    public bool IsBreakable => !float.IsPositiveInfinity(SecondsToBreak);

    /// <summary>
    /// The one kind of tool this comes apart faster under, or null for a block no tool is any better at than
    /// a bare hand. Wood answers to an axe, earth to a shovel, stone and ore to a pickaxe; a flower answers
    /// to nothing, and is torn out just as fast either way.
    /// </summary>
    public ToolKind? HarvestTool { get; protected set; }

    /// <summary>
    /// How deep this is buried: the lowest <see cref="ToolMaterial.HarvestLevel"/> that earns anything from
    /// it. Zero for almost everything, which any tool of the right kind clears.
    /// </summary>
    public int HarvestLevel { get; protected set; }

    /// <summary>
    /// Whether the right tool is what makes the difference between a drop and nothing. Stone comes apart
    /// under bare hands and leaves no cobblestone behind; dirt does not care what dug it.
    /// <para>
    /// Kept apart from <see cref="HarvestLevel"/> because the two answer different questions. This one asks
    /// whether a tool was needed at all, and the level asks whether the one brought was good enough; a block
    /// that needs no tool has nothing to be too poor for.
    /// </para>
    /// </summary>
    public bool RequiresCorrectTool { get; protected set; }

    protected Block(ushort id)
    {
        Id = id;
        _fullBlockAABBList[0] = _fullBlockAABB;
    }

    public abstract BlockState GetNewDefaultState();

    /// <summary>
    /// What breaking this block leaves behind, or an empty stack when it leaves nothing. Itself for almost
    /// everything; the exceptions are the blocks that come apart on the way out — stone into cobblestone,
    /// grass into the dirt under it, ore into what was buried in it — and the greenery, which is torn rather
    /// than harvested.
    /// <para>
    /// Only asked when a player broke the block with a swing the tool was good enough for; see
    /// <see cref="Harvesting.CanHarvest"/>, which is the gate in front of this. Water washing a flower away,
    /// sand settling a cell lower and a blast taking a hillside apart all go through the same removal, and
    /// none of them should leave a pile of anything behind.
    /// </para>
    /// </summary>
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

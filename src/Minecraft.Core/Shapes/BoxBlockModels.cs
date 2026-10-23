using Minecraft.Core.Textures;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public sealed class BlockModelSlab(TextureAtlas textureAtlas, Vector2 side, Vector2 top, Vector2 bottom)
    : BoxBlockModel(textureAtlas)
{
    public BlockModelSlab(TextureAtlas textureAtlas, Vector2 cell) : this(textureAtlas, cell, cell, cell)
    {
    }

    protected override IEnumerable<Box> BoxesFor(BlockState state) =>
        BlockShapes.Slab(state is BlockStateSlab { IsTop: true });

    protected override Vector2 CellFor(BlockState state, Direction face) => face switch
    {
        Direction.Top => top,
        Direction.Bottom => bottom,
        _ => side,
    };
}

public sealed class BlockModelStairs(TextureAtlas textureAtlas, Vector2 cell) : BoxBlockModel(textureAtlas)
{
    protected override IEnumerable<Box> BoxesFor(BlockState state) =>
        state is BlockStateStairs stairs
            ? BlockShapes.Stairs(stairs.Facing, stairs.IsUpsideDown)
            : BlockShapes.Stairs(Direction.Front, upsideDown: false);

    protected override Vector2 CellFor(BlockState state, Direction face) => cell;
}

public sealed class BlockModelDoor(TextureAtlas textureAtlas) : BoxBlockModel(textureAtlas)
{
    protected override IEnumerable<Box> BoxesFor(BlockState state) =>
        BlockShapes.Door(state is BlockStateDoor door ? door.PanelSide : Direction.Front);

    protected override Vector2 CellFor(BlockState state, Direction face) =>
        state is BlockStateDoor { IsUpper: true } ? BlockAtlas.DoorTop : BlockAtlas.DoorBottom;
}

public sealed class BlockModelLadder(TextureAtlas textureAtlas) : BoxBlockModel(textureAtlas)
{
    protected override IEnumerable<Box> BoxesFor(BlockState state) =>
        BlockShapes.Ladder(state is BlockStateLadder ladder ? ladder.Attachment : Direction.Front);

    protected override Vector2 CellFor(BlockState state, Direction face) => BlockAtlas.Ladder;
}

public sealed class BlockModelFence(TextureAtlas textureAtlas) : BoxBlockModel(textureAtlas)
{
    protected override IEnumerable<Box> BoxesFor(BlockState state) =>
        state is BlockStateConnecting connecting ? BlockShapes.Fence(connecting) : [];

    protected override Vector2 CellFor(BlockState state, Direction face) => BlockAtlas.Planks;
}

public sealed class BlockModelGlassPane(TextureAtlas textureAtlas) : BoxBlockModel(textureAtlas)
{
    protected override IEnumerable<Box> BoxesFor(BlockState state) =>
        state is BlockStateConnecting connecting ? BlockShapes.Pane(connecting) : [];

    protected override Vector2 CellFor(BlockState state, Direction face) => BlockAtlas.Glass;
}

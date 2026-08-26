using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

/// <summary>
/// Draws whatever the player is holding, in front of them and off to one side.
/// <para>
/// What is held is the selected hotbar slot, so this and the bar along the bottom of the screen are two views
/// of the same thing: the slot says what and how many are left of it, and this says what it looks like in the
/// hand carrying it.
/// </para>
/// <para>
/// A block is held the way a block is held: turned onto a corner, big enough to fill a fist. A tool is a flat
/// piece of artwork and is held out at a slant instead, tilted just off the screen so that it reads as
/// something being carried rather than as a sticker on the glass.
/// </para>
/// </summary>
public sealed class HeldItemRenderer
{
    /// <summary>
    /// How wide a view the hand is drawn through. Fixed rather than the player's own, so that widening the
    /// field of view opens up the world without also throwing the held block off the corner of the screen.
    /// </summary>
    private const float HandFieldOfView = 1.22F;

    /// <summary>How big the block is drawn, as a share of a real one.</summary>
    private const float Scale = 0.42F;

    /// <summary>The same for a flat item, which has to be bigger to read at all, and is drawn edge on to nothing.</summary>
    private const float SpriteScale = 0.62F;

    /// <summary>
    /// How far a held sprite is turned. Yawed away from the eye and rolled so the head of a tool is uppermost,
    /// which between them stop it looking like a card held up to the screen.
    /// </summary>
    private const float SpriteYaw = 0.62F;
    private const float SpriteRoll = -0.5F;

    /// <summary>
    /// Where it sits in front of the eye: to the right, below the middle, and far enough forward to clear the
    /// near plane. Held in the same space the projection is built in, since the view matrix for this pass is
    /// the identity, which is what pins the block to the screen rather than to the world.
    /// </summary>
    private static readonly Vector3 RestingPosition = new(0.58F, -0.46F, -1.05F);

    /// <summary>Turned so that a corner faces the player and the top of the block is visible over it.</summary>
    private const float RestingYaw = 0.72F;
    private const float RestingPitch = -0.22F;

    /// <summary>How long one swing takes, from the block dropping out of view to it being back at rest.</summary>
    private const float SwingSeconds = 0.28F;

    /// <summary>How far down and back a swing takes the block at its lowest.</summary>
    private const float SwingDrop = 0.42F;
    private const float SwingTwist = 0.9F;

    /// <summary>How far the block sways as the player walks, and how far they walk between one sway and the next.</summary>
    private const float BobAmount = 0.022F;
    private const float BobStrideBlocks = 1.4F;

    /// <summary>
    /// Movement in one frame beyond which the player was put somewhere rather than having walked there.
    /// Spawning covers an enormous distance in a single frame and would otherwise start a bob with it.
    /// </summary>
    private const float TeleportDistance = 4F;

    private readonly Game _game;
    private readonly BasicShader _shader;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly TextureAtlas _textureAtlas;
    private readonly TextureAtlas _itemAtlas;

    private VAOModel? _model;

    /// <summary>What the current mesh was built for. A mesh is only rebuilt when one of these has moved.</summary>
    private Item? _meshedItem;
    private BlockState? _meshedState;
    private uint _meshedLight;

    /// <summary>Whether what is in hand is drawn as a flat sprite rather than as a block.</summary>
    private bool _meshedIsSprite;

    private Matrix4 _projectionMatrix = Matrix4.Identity;

    /// <summary>How far through a swing we are, from zero at rest to one at the end of it.</summary>
    private float _swing;

    /// <summary>How far the player has walked, which the sway is a function of.</summary>
    private float _walkedDistance;
    private Vector3 _lastPlayerPosition;
    private bool _hasLastPosition;

    public HeldItemRenderer(
        Game game,
        BasicShader shader,
        BlockModelRegistry blockModelRegistry,
        TextureAtlas textureAtlas,
        TextureAtlas itemAtlas)
    {
        _game = game;
        _shader = shader;
        _blockModelRegistry = blockModelRegistry;
        _textureAtlas = textureAtlas;
        _itemAtlas = itemAtlas;

        game.ClientPlayer.OnSwingHandler += OnSwing;
        OnWindowResized(game.Window.ClientSize.X, game.Window.ClientSize.Y);
    }

    /// <summary>Starts a swing, or restarts one already under way so a fast click still reads as two blows.</summary>
    private void OnSwing() => _swing = 0.0001F;

    /// <summary>
    /// Forgets where the player was, since the next world starts them somewhere else entirely and the step
    /// between the two is not one they walked.
    /// </summary>
    public void OnWorldUnloaded()
    {
        _hasLastPosition = false;
        _walkedDistance = 0F;
        _swing = 0F;
    }

    public void OnWindowResized(int pixelWidth, int pixelHeight)
    {
        _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
            HandFieldOfView,
            pixelWidth / (float)Math.Max(1, pixelHeight),
            0.05F,
            10F);
    }

    /// <summary>Advances the swing and the walking sway. Called once per frame whether or not anything is drawn.</summary>
    public void Update(float deltaTime)
    {
        if (_swing > 0F)
        {
            _swing += deltaTime / SwingSeconds;
            if (_swing >= 1F)
            {
                _swing = 0F;
            }
        }

        Vector3 position = _game.ClientPlayer.Position;
        if (_hasLastPosition)
        {
            Vector3 movement = position - _lastPlayerPosition;
            float travelled = new Vector2(movement.X, movement.Z).Length;

            if (travelled <= TeleportDistance)
            {
                _walkedDistance += travelled;
            }
        }

        _lastPlayerPosition = position;
        _hasLastPosition = true;
    }

    /// <summary>
    /// Draws the held block over whatever has already been laid down. The depth buffer is cleared first, so
    /// the block is never cut into by the world it is being held up in front of.
    /// <para>
    /// Spliced in between the world and the interface, both of which are drawn with depth testing and culling
    /// off, so this puts them back for itself and takes them off again on the way out.
    /// </para>
    /// </summary>
    public void Render(World world, Camera activeCamera)
    {
        // A hand held up in front of the eye only makes sense from behind that eye. The detached overhead
        // camera is not looking out of it at all, and a third person view can see the body carrying the
        // block, which would otherwise also be pinned to the screen in front of it.
        if (activeCamera != _game.ClientPlayer.Camera ||
            _game.ClientPlayer.Perspective != CameraPerspective.FirstPerson)
        {
            return;
        }

        ItemStack held = _game.ClientPlayer.Inventory.Selected;
        if (held.IsEmpty || held.Block == BlockRegistry.Air)
        {
            return;
        }

        RebuildMeshIfStale(world, held);
        if (_model is null)
        {
            return;
        }

        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);

        // A sprite carries both of its faces and is meant to be seen from either, so culling is only put on
        // for the blocks, which are closed shapes with insides nobody should see.
        if (_meshedIsSprite)
        {
            GL.Disable(EnableCap.CullFace);
        }
        else
        {
            GL.Enable(EnableCap.CullFace);
        }

        _shader.Start();
        _shader.LoadTexture(
            _shader.LocationTextureAtlas,
            0,
            _meshedIsSprite ? _itemAtlas.Id : _textureAtlas.Id);
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, _projectionMatrix);

        // The identity view matrix is what fixes the block to the screen: its vertices are already where the
        // eye is looking from, so turning the head moves the world past it and leaves it where it was.
        _shader.LoadMatrix(_shader.LocationViewMatrix, Matrix4.Identity);
        _shader.LoadMatrix(_shader.LocationTransformationMatrix, BuildTransformation());

        _shader.LoadVector(_shader.LocationSunColor, world.Environment.GetCurrentSunColor());
        _shader.LoadVector(_shader.LocationAmbientColor, world.Environment.AmbientColor);
        _shader.LoadFloat(_shader.LocationMaterialAlpha, 1.0F);

        // Held at the eye, which is the one place in the world distance haze cannot reach, so the fog is
        // measured from the block itself and comes out as none of it.
        _shader.LoadVector(_shader.LocationCameraPosition, Vector3.Zero);
        _shader.LoadVector(_shader.LocationFogColor, Vector3.Zero);
        _shader.LoadFloat(_shader.LocationFogStart, 1000F);
        _shader.LoadFloat(_shader.LocationFogEnd, 2000F);

        _model.BindVAO();
        GL.DrawArrays(PrimitiveType.Triangles, 0, _model.IndicesCount);
        VAOModel.UnbindVAO();

        // Put back what the rest of the frame was drawn with, since this pass is spliced into the middle of it.
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, activeCamera.CurrentProjectionMatrix);

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
    }

    /// <summary>
    /// Where the block sits this frame: at rest, plus whatever the swing and the walking sway are adding.
    /// </summary>
    private Matrix4 BuildTransformation()
    {
        // Up and back down over the length of a swing, so the block dips out of view and returns rather than
        // snapping back to where it started.
        float swingArc = MathF.Sin(_swing * MathF.PI);

        float bobPhase = _walkedDistance / BobStrideBlocks * MathF.PI;

        // Twice the rate horizontally: a stride throws the hand to one side and back on every step, but rises
        // and falls once per pair of them, which is what reads as walking rather than as swaying.
        var bob = new Vector3(
            MathF.Sin(bobPhase * 2F) * BobAmount,
            -MathF.Abs(MathF.Sin(bobPhase)) * BobAmount,
            0F);

        Vector3 position = RestingPosition + bob + new Vector3(0F, -SwingDrop * swingArc, 0.14F * swingArc);

        if (_meshedIsSprite)
        {
            return Matrix4.CreateScale(SpriteScale)
                   * Matrix4.CreateRotationZ(SpriteRoll)
                   * Matrix4.CreateRotationX(RestingPitch - (SwingTwist * swingArc))
                   * Matrix4.CreateRotationY(SpriteYaw)
                   * Matrix4.CreateTranslation(position);
        }

        return Matrix4.CreateScale(Scale)
               * Matrix4.CreateRotationX(RestingPitch - (SwingTwist * swingArc))
               * Matrix4.CreateRotationY(RestingYaw)
               * Matrix4.CreateTranslation(position);
    }

    private void RebuildMeshIfStale(World world, ItemStack held)
    {
        Light light = SampleLightAtPlayer(world);
        uint packedLight = light.GetStorage();

        // A sprite is lit flat and is the same mesh wherever it is being carried, so only a change of item
        // matters for one. A block is lit by where the player is standing, and its state matters as well as
        // the block: a torch turned to face a wall is a different shape from one standing up.
        if (_model is not null &&
            _meshedItem == held.Item &&
            (_meshedIsSprite || _meshedLight == packedLight))
        {
            return;
        }

        // Built here rather than above the check, since a block that carries its own state hands back a fresh
        // one every time it is asked and the check is reached on every frame the same thing is being carried.
        BlockState? state = held.Block is null ? null : BlockRegistry.GetState(held.Block);

        _model?.CleanUp();

        _model = held.Item switch
        {
            SpriteItem sprite => ItemSpriteMesh.Build(_itemAtlas, sprite.IconCell),
            _ => BlockIconMesh.Build(_blockModelRegistry, state!, light),
        };

        _meshedIsSprite = held.Item is SpriteItem;
        _meshedItem = held.Item;
        _meshedState = state;
        _meshedLight = packedLight;
    }

    /// <summary>
    /// The light where the player is standing, which is what the block in their hand is lit by. A cell in a
    /// chunk that is not loaded is treated as open daylight, the same stand in the chunk mesher uses.
    /// </summary>
    private Light SampleLightAtPlayer(World world)
    {
        Vector3i blockPos = _game.ClientPlayer.Camera.Position.ToBlockPos();

        if (!world.IsOutsideBuildHeight(blockPos.Y) &&
            world.LoadedChunks.TryGetValue(
                World.GetChunkPosition(blockPos.X, blockPos.Z),
                out Chunk? chunk))
        {
            Vector3i local = blockPos.ToChunkLocal();
            return chunk.LightMap.GetLightColorAt(
                (uint)local.X,
                (uint)local.Y,
                (uint)local.Z,
                BlockIconMesh.LightScale);
        }

        return BlockIconMesh.FullDaylight;
    }

    public void CleanUp()
    {
        _game.ClientPlayer.OnSwingHandler -= OnSwing;
        _model?.CleanUp();
        _model = null;
    }
}

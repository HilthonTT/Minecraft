using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

/// <summary>
/// Draws the block the player is holding, in front of them and off to one side.
/// <para>
/// This is also the only place what is selected is shown at all. There is no bar of slots along the bottom of
/// the screen, because there is no inventory behind one: what a player is carrying is simply in their hand.
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

    // The same shading by orientation the chunk mesher uses, so a block in the hand is lit like one in the
    // world rather than reading as a flat silhouette of itself.
    private const uint TopBrightness = 60;
    private const uint BottomBrightness = 36;
    private const uint SideXBrightness = 44;
    private const uint SideZBrightness = 52;

    /// <summary>Lightmap values are 0..15 while the packed channels are 0..63, so samples are scaled up.</summary>
    private const uint LightScale = 4;

    private readonly Game _game;
    private readonly BasicShader _shader;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly TextureAtlas _textureAtlas;

    private VAOModel? _model;

    /// <summary>What the current mesh was built for. A mesh is only rebuilt when one of these has moved.</summary>
    private Block? _meshedBlock;
    private BlockState? _meshedState;
    private uint _meshedLight;

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
        TextureAtlas textureAtlas)
    {
        _game = game;
        _shader = shader;
        _blockModelRegistry = blockModelRegistry;
        _textureAtlas = textureAtlas;

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
        // Nobody is holding anything in the detached overhead camera, which is not looking out of the
        // player's eyes at all.
        if (activeCamera != _game.ClientPlayer.Camera)
        {
            return;
        }

        BlockState held = _game.ClientPlayer.SelectedBlock;
        if (held.GetBlock() == BlockRegistry.Air)
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
        GL.Enable(EnableCap.CullFace);
        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);

        _shader.Start();
        _shader.LoadTexture(_shader.LocationTextureAtlas, 0, _textureAtlas.Id);
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

        return Matrix4.CreateScale(Scale)
               * Matrix4.CreateRotationX(RestingPitch - (SwingTwist * swingArc))
               * Matrix4.CreateRotationY(RestingYaw)
               * Matrix4.CreateTranslation(position);
    }

    private void RebuildMeshIfStale(World world, BlockState held)
    {
        Light light = SampleLightAtPlayer(world);
        uint packedLight = light.GetStorage();

        // The state matters as well as the block: a torch turned to face a wall is a different shape from one
        // standing up, and both are the same block.
        if (_model is not null &&
            _meshedBlock == held.GetBlock() &&
            ReferenceEquals(_meshedState, held) &&
            _meshedLight == packedLight)
        {
            return;
        }

        _model?.CleanUp();
        _model = BuildModel(held, light);
        _meshedBlock = held.GetBlock();
        _meshedState = held;
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
            return chunk.LightMap.GetLightColorAt((uint)local.X, (uint)local.Y, (uint)local.Z, LightScale);
        }

        return new Light(0, 0, 0, 15 * LightScale, 0);
    }

    /// <summary>
    /// Builds the block as a mesh of its own. Every face of it is taken, hidden ones included, since there is
    /// no neighbouring block in a hand for any of them to be buried against.
    /// </summary>
    private VAOModel BuildModel(BlockState state, Light light)
    {
        BlockModel model = _blockModelRegistry.Models[state.GetBlock().Id];
        List<BlockFace> faces = [];

        foreach (Direction direction in Enum.GetValues<Direction>())
        {
            faces.AddRange(model.GetPartialVisibleFaces(state, Vector3i.Zero, direction));
        }

        faces.AddRange(model.GetAlwaysVisibleFaces(state, Vector3i.Zero));

        int quadCount = faces.Count * (model.DoubleSidedFaces ? 2 : 1);
        int vertexCount = quadCount * 6;

        var layout = new ChunkBufferLayout
        {
            VertexPositions = new float[vertexCount * 3],
            VertexNormals = new float[vertexCount * 3],
            VertexUVs = new float[vertexCount * 2],
            VertexLights = new uint[vertexCount],
        };

        foreach (BlockFace face in faces)
        {
            AddQuad(ref layout, face, light, flipWinding: false);

            if (model.DoubleSidedFaces)
            {
                AddQuad(ref layout, face, light, flipWinding: true);
            }
        }

        layout.IndicesCount = vertexCount;
        return new VAOModel(layout);
    }

    private static void AddQuad(ref ChunkBufferLayout layout, BlockFace face, Light light, bool flipWinding)
    {
        ReadOnlySpan<int> order = flipWinding ? [1, 0, 3, 1, 3, 2] : [0, 1, 2, 0, 2, 3];

        Vector3 normal = flipWinding ? -face.Normal : face.Normal;

        light.SetBrightness(BrightnessFor(normal));
        uint packedLight = light.GetStorage();

        foreach (int index in order)
        {
            // Pulled back onto its own middle, so that turning the block spins it on the spot rather than
            // swinging it around the corner its model is built from.
            Vector3 position = face.Positions[index] - new Vector3(0.5F, 0.5F, 0.5F);

            layout.VertexPositions[layout.PositionsPointer++] = position.X;
            layout.VertexPositions[layout.PositionsPointer++] = position.Y;
            layout.VertexPositions[layout.PositionsPointer++] = position.Z;

            layout.VertexNormals[layout.NormalsPointer++] = normal.X;
            layout.VertexNormals[layout.NormalsPointer++] = normal.Y;
            layout.VertexNormals[layout.NormalsPointer++] = normal.Z;

            layout.VertexUVs[layout.UVsPointer++] = face.TextureCoords[index].X;
            layout.VertexUVs[layout.UVsPointer++] = face.TextureCoords[index].Y;

            layout.VertexLights[layout.LightsPointer++] = packedLight;
        }
    }

    /// <summary>Which of the fixed face shades an outward pointing normal falls under.</summary>
    private static uint BrightnessFor(Vector3 normal)
    {
        if (normal.LengthSquared < 0.0001F)
        {
            return SideZBrightness;
        }

        Vector3 unit = Vector3.Normalize(normal);

        if (unit.Y > 0.5F)
        {
            return TopBrightness;
        }

        if (unit.Y < -0.5F)
        {
            return BottomBrightness;
        }

        return MathF.Abs(unit.X) > MathF.Abs(unit.Z) ? SideXBrightness : SideZBrightness;
    }

    public void CleanUp()
    {
        _game.ClientPlayer.OnSwingHandler -= OnSwing;
        _model?.CleanUp();
        _model = null;
    }
}

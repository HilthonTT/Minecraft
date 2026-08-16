using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Games;
using Minecraft.Core.Physics;
using Minecraft.Core.Render.MeshGenerator;
using Minecraft.Core.Render.Particles;
using Minecraft.Core.Render.UI;
using Minecraft.Core.Render.UI.Presets;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shaders.EntityShader;
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
/// Draws the world. Chunk meshes are built on a background thread and handed over one at a time, so that a
/// chunk changing never stalls a frame.
/// </summary>
public sealed class MasterRenderer
{
    private struct ChunkRemeshLayout
    {
        public Vector2 ChunkGridPosition;
        public ChunkMesh Mesh;
    }

    // The colours the framebuffer is cleared with.
    private const float ColorClearR = 0.02F;
    private const float ColorClearG = 0.01F;
    private const float ColorClearB = 0.03F;

    /// <summary>How much of what is behind the water still comes through it.</summary>
    private const float WaterAlpha = 0.72F;

    /// <summary>Fully opaque, which is every pass except the water.</summary>
    private const float SolidAlpha = 1.0F;

    /// <summary>
    /// How far towards red a mob goes while it is showing a blow. Short of the whole way, so what is seen is
    /// the animal under a red wash rather than a red silhouette of it.
    /// </summary>
    private const float HurtFlashStrength = 0.35F;

    /// <summary>
    /// The colour looking through water settles into. Deliberately not the colour of the water surface: what
    /// is being modelled is a long way of it stacked up between the eye and whatever is being looked at.
    /// </summary>
    private static readonly Vector3 UnderwaterTint = new(0.02F, 0.16F, 0.32F);

    /// <summary>
    /// How far can be seen underwater, in blocks. Short, because it is the drop from open air to this that
    /// reads as having gone under rather than the colour on its own.
    /// </summary>
    private const float UnderwaterFogStart = 0.5F;
    private const float UnderwaterFogEnd = 22F;

    /// <summary>The fog in use this frame, which is the sky's unless the camera is under water.</summary>
    private Vector3 _fogColor;
    private float _fogStart;
    private float _fogEnd;

    /// <summary>Whether the camera is inside a liquid, which changes both the fog and the sky.</summary>
    private bool _cameraSubmerged;

    private readonly Game _game;

    private readonly BasicShader _basicShader;
    private readonly EntityShader _entityShader;
    private readonly CameraController _cameraController;
    private readonly WireframeRenderer _wireframeRenderer;
    private readonly PlayerHoverBlockRenderer _playerBlockRenderer;
    private readonly TextureAtlas _textureAtlas;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly EntityMeshRegistry _entityMeshRegistry;
    private readonly ScreenQuad _screenQuad;
    private readonly UIRenderer _uiRenderer;
    private readonly Skydome _skydome;
    private readonly ChunkMeshGenerator _blocksMeshGenerator;

    /// <summary>The chunks that are currently being rendered.</summary>
    private readonly Dictionary<Vector2, RenderChunk> _toRenderChunks = [];

    /// <summary>The chunks awaiting a remesh, as a queue plus a set so membership tests stay cheap.</summary>
    private readonly LinkedList<Chunk> _toRemeshChunksQueue = new();
    private readonly HashSet<Chunk> _toRemeshChunksSet = [];

    /// <summary>
    /// The finished mesh waiting to be uploaded. Only one is held at a time, so that the mesh generator can
    /// keep reusing the same large arrays instead of copying them per chunk.
    /// </summary>
    private ChunkRemeshLayout _availableChunkMesh;
    private bool _chunkAvailableToRemesh;

    /// <summary>
    /// Counts the worlds that have been loaded. A mesh is built against the chunks of one of them, so one
    /// that comes back after its world was left is dropped rather than drawn over the next world.
    /// </summary>
    private int _worldGeneration;

    private readonly Lock _meshLock = new();
    private readonly Thread _meshGenerationThread;
    private volatile bool _isRunning = true;

    private readonly HeldItemRenderer _heldItemRenderer;
    private readonly DroppedItemRenderer _droppedItemRenderer;
    private readonly ParticleSystem _particleSystem = new();
    private readonly ParticleRenderer _particleRenderer;

    /// <summary>What throws specks into the air, and what the world tells when something happens worth one.</summary>
    public ParticleDirector Particles { get; }

    public DebugHelper DebugHelper { get; }
    public UICanvasIngame IngameCanvas { get; }
    public UICanvasHotbar HotbarCanvas { get; }
    public UICanvasInventory InventoryCanvas { get; }

    /// <summary>
    /// What draws a block inside a slot. The screens that own slots queue their icons with it while the
    /// interface is being built, and it draws them all in a pass of its own between the two halves of it.
    /// </summary>
    public BlockIconRenderer BlockIcons { get; }

    public int DitherTextureId { get; }

    public MasterRenderer(Game game)
    {
        _game = game;
        _basicShader = new BasicShader();
        _entityShader = new EntityShader();
        _cameraController = new CameraController(game, game.ClientPlayer.Camera);

        SetActiveCamera(game.ClientPlayer.Camera);

        int textureAtlasId = TextureLoader.LoadBlockAtlas(
            Assets.Path("Resources/texturePack.png"),
            BlockAtlas.CutOutCells,
            BlockAtlas.CellsPerRow);
        _textureAtlas = new TextureAtlas(textureAtlasId, BlockAtlas.SizeInPixels, BlockAtlas.CellSizeInPixels);
        _blockModelRegistry = new BlockModelRegistry(_textureAtlas);
        _blocksMeshGenerator = new ChunkMeshGenerator(_blockModelRegistry);
        _entityMeshRegistry = new EntityMeshRegistry();
        _screenQuad = new ScreenQuad(game.Window);
        _wireframeRenderer = new WireframeRenderer(this);
        DebugHelper = new DebugHelper(game, _wireframeRenderer);
        _playerBlockRenderer = new PlayerHoverBlockRenderer(_wireframeRenderer, game.ClientPlayer);
        DitherTextureId = TextureLoader.LoadDitherTexture();
        _skydome = new Skydome(game);
        _heldItemRenderer = new HeldItemRenderer(game, _basicShader, _blockModelRegistry, _textureAtlas);
        _droppedItemRenderer = new DroppedItemRenderer(_basicShader, _blockModelRegistry, _textureAtlas);
        _particleRenderer = new ParticleRenderer(_basicShader, _textureAtlas);
        Particles = new ParticleDirector(game, _particleSystem, _blockModelRegistry);

        BlockIcons = new BlockIconRenderer(
            _basicShader,
            _blockModelRegistry,
            _textureAtlas,
            game.Window.ClientSize.X,
            game.Window.ClientSize.Y);

        _uiRenderer = new UIRenderer(_cameraController);
        IngameCanvas = new UICanvasIngame(game);
        AddCanvas(IngameCanvas);

        // Registered before the menu canvases, which are added by the menu controller once this renderer
        // exists, so that a screen brought up over the world is drawn over the bar rather than under it. The
        // overlays go on the same list and are picked out of it by the pass that draws them.
        HotbarCanvas = new UICanvasHotbar(game);
        AddCanvas(HotbarCanvas);
        AddCanvas(HotbarCanvas.Overlay);

        InventoryCanvas = new UICanvasInventory(game);
        AddCanvas(InventoryCanvas);
        AddCanvas(InventoryCanvas.Overlay);

        HotbarCanvas.IsEnabled = false;
        InventoryCanvas.IsEnabled = false;

        EnableDepthTest();
        EnableCulling();

        _meshGenerationThread = new Thread(RunMeshGeneration)
        {
            IsBackground = true,
            Name = "Chunk meshing",
        };
        _meshGenerationThread.Start();
    }

    public Camera GetActiveCamera() => _cameraController.Camera;

    public void AddCanvas(UICanvas canvas) => _uiRenderer.AddCanvas(canvas);

    public void RemoveCanvas(UICanvas canvas) => _uiRenderer.RemoveCanvas(canvas);

    public void SetActiveCamera(Camera camera)
    {
        if (_cameraController.Camera is not null)
        {
            _cameraController.Camera.OnProjectionChangedHandler -= OnPlayerCameraProjectionChanged;
        }

        camera.OnProjectionChangedHandler += OnPlayerCameraProjectionChanged;
        _cameraController.ControlCamera(camera);
        UploadActiveCameraProjectionMatrix();
    }

    public void Render(World world)
    {
        UpdateFog(world);

        GL.Enable(EnableCap.DepthTest);
        _screenQuad.Bind();

        // Under water the clear colour is what shows wherever no block was drawn, which is why the sky is
        // left out below: looking up from the bottom of a sea should not find a bright horizon over it.
        if (_cameraSubmerged)
        {
            GL.ClearColor(_fogColor.X, _fogColor.Y, _fogColor.Z, 1.0F);
        }
        else
        {
            GL.ClearColor(ColorClearR, ColorClearG, ColorClearB, 1.0F);
        }

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (!_cameraSubmerged)
        {
            _skydome.Render();
        }

        RenderChunks(world);
        RenderEntities(world);

        // After the mobs, since it puts back the program and the texture the entity pass leaves bound, and
        // before the water, so a stack that has fallen into a stream is seen through its surface.
        _droppedItemRenderer.Render(world, _cameraController.Camera, _fogColor, _fogStart, _fogEnd);

        // Before the water, so that a splash thrown up out of a lake is seen through its surface rather than
        // painted over it, and after the solid world, which is what hides the specks behind terrain.
        _particleRenderer.Render(_particleSystem, _cameraController.Camera, world, _fogColor, _fogStart, _fogEnd);

        RenderWater();

        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        _playerBlockRenderer.RenderSelection();
        DebugHelper.UpdateAndRender();

        // Over the world and its outlines, under the interface: what the player is holding is part of the
        // scene, but nothing they can read should end up behind it.
        _heldItemRenderer.Render(world, _cameraController.Camera);

        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        RenderInterface();
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);

        _screenQuad.Unbind();
        _screenQuad.RenderToScreen();
    }

    /// <summary>
    /// Draws the interface on its own, over a cleared screen. This is what the main menu looks like, where
    /// there is no world loaded to draw behind it.
    /// </summary>
    public void RenderInterfaceOnly()
    {
        _screenQuad.Bind();
        GL.ClearColor(ColorClearR, ColorClearG, ColorClearB, 1.0F);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        RenderInterface();
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);

        _screenQuad.Unbind();
        _screenQuad.RenderToScreen();
    }

    /// <summary>
    /// The interface, in three parts: the panels, then the blocks standing in whichever of them are slots,
    /// then the counts and labels that have to be read over those blocks.
    /// <para>
    /// The middle part is real geometry with a depth buffer of its own, which is why it cannot simply be
    /// another component on a canvas — a canvas is a stack of flat quads drawn in the order it was given
    /// them, and a cube needs to know which of its own faces is in front.
    /// </para>
    /// </summary>
    private void RenderInterface()
    {
        _uiRenderer.Render();

        BlockIcons.Render(_cameraController.Camera);

        // The icon pass leaves the blend state it found, but takes the depth test with it, so what follows is
        // drawn under the same conditions the canvases above were.
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _uiRenderer.RenderOverlays();
    }

    /// <summary>
    /// Drops everything that belonged to the world that was just left, so that the next one starts from an
    /// empty renderer rather than showing what is left of the last.
    /// </summary>
    public void UnloadWorld()
    {
        lock (_meshLock)
        {
            _toRemeshChunksQueue.Clear();
            _toRemeshChunksSet.Clear();
            _chunkAvailableToRemesh = false;

            // Anything the meshing thread is part way through belongs to the world being left, and is
            // thrown away rather than uploaded once it comes back.
            _worldGeneration++;
        }

        foreach (KeyValuePair<Vector2, RenderChunk> chunkToRender in _toRenderChunks)
        {
            chunkToRender.Value.CleanUp();
        }

        _toRenderChunks.Clear();

        // World space canvases are the name tags above other players, who are gone with the world.
        _uiRenderer.RemoveCanvassesIn(RenderSpace.World);

        IngameCanvas.OnWorldUnloaded();
        HotbarCanvas.OnWorldUnloaded();
        DebugHelper.OnWorldUnloaded();
        _heldItemRenderer.OnWorldUnloaded();
        Particles.OnWorldUnloaded();

        // Anything queued on the last frame of the world being left would otherwise be drawn over the menu.
        BlockIcons.Clear();
    }

    /// <summary>
    /// Throws away the mouse movement built up since the cursor was last grabbed. Called when the controls
    /// are handed back to the player, so that closing a menu does not also spin the camera.
    /// </summary>
    public void DiscardPendingMouseLook() => _cameraController.DiscardPendingMouseLook();

    /// <summary>
    /// The distance the fog is taken over, in blocks. Measured against whatever the player has set the render
    /// distance to rather than a fixed figure, so it always closes over exactly where the loaded world runs
    /// out and moving the slider moves the horizon rather than leaving the haze somewhere else.
    /// </summary>
    private float FogStartDistance => _game.Settings.RenderDistanceBlocks * Constants.FOG_START_FRACTION;

    private float FogEndDistance => _game.Settings.RenderDistanceBlocks * Constants.FOG_END_FRACTION;

    /// <summary>
    /// Works out what the fog does this frame. Normally it is the sky's own, closing over at the edge of the
    /// loaded world; inside water it becomes a short, dim blue that is what actually reads as being under.
    /// </summary>
    private void UpdateFog(World world)
    {
        _cameraSubmerged = IsPositionInLiquid(world, _cameraController.Camera.Position);

        if (!_cameraSubmerged)
        {
            _fogColor = world.Environment.GetCurrentFogColor();
            _fogStart = FogStartDistance;
            _fogEnd = FogEndDistance;
            return;
        }

        // Dimmed by how bright it is outside, so that going under at night is dark rather than the same
        // blue it would be at noon.
        Vector3 skyColor = world.Environment.GetCurrentFogColor();
        float daylight = (skyColor.X + skyColor.Y + skyColor.Z) / 3.0F;

        _fogColor = UnderwaterTint * Math.Clamp(daylight * 1.6F, 0.08F, 1.0F);
        _fogStart = UnderwaterFogStart;
        _fogEnd = UnderwaterFogEnd;
    }

    private static bool IsPositionInLiquid(World world, Vector3 position)
    {
        var blockPos = position.ToBlockPos();
        if (world.IsOutsideBuildHeight(blockPos.Y))
        {
            return false;
        }

        return world.GetBlockAt(blockPos).GetBlock().IsLiquid;
    }

    private void RenderChunks(World world)
    {
        _basicShader.Start();
        _basicShader.LoadTexture(_basicShader.LocationTextureAtlas, 0, _textureAtlas.Id);
        _basicShader.LoadMatrix(_basicShader.LocationViewMatrix, _cameraController.Camera.CurrentViewMatrix);
        _basicShader.LoadVector(_basicShader.LocationSunColor, world.Environment.GetCurrentSunColor());
        _basicShader.LoadVector(_basicShader.LocationAmbientColor, world.Environment.AmbientColor);

        // The active camera rather than the player's, so that the detached overhead camera sees the world
        // fog from where it is actually looking at it from.
        _basicShader.LoadVector(_basicShader.LocationCameraPosition, _cameraController.Camera.Position);
        _basicShader.LoadVector(_basicShader.LocationFogColor, _fogColor);
        _basicShader.LoadFloat(_basicShader.LocationFogStart, _fogStart);
        _basicShader.LoadFloat(_basicShader.LocationFogEnd, _fogEnd);
        _basicShader.LoadFloat(_basicShader.LocationMaterialAlpha, SolidAlpha);

        DrawChunkModels(liquid: false);
    }

    /// <summary>
    /// Draws the water, after the solid blocks and the entities so that everything it is meant to be seen
    /// through has already been laid down for it to blend over.
    /// </summary>
    private void RenderWater()
    {
        // The entity pass ran in between and left its own program bound and its own skin on texture unit
        // zero, so both have to be put back before the water is drawn with them.
        _basicShader.Start();
        _basicShader.LoadTexture(_basicShader.LocationTextureAtlas, 0, _textureAtlas.Id);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Depth writes off, so that one stretch of water does not cut a hole in the stretch behind it: they
        // are all the same surface as far as the eye is concerned, and the depth test against the solid
        // world has already decided which of them is visible at all.
        GL.DepthMask(false);

        // The surface of a sea is a single skin of quads, all facing up. Culling would leave it invisible
        // from underneath, which is exactly where a swimmer looks at it from.
        GL.Disable(EnableCap.CullFace);

        _basicShader.LoadFloat(_basicShader.LocationMaterialAlpha, WaterAlpha);
        DrawChunkModels(liquid: true);

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    /// <summary>Draws one of the two meshes of every chunk that survives the view frustum.</summary>
    private void DrawChunkModels(bool liquid)
    {
        foreach (KeyValuePair<Vector2, RenderChunk> chunkToRender in _toRenderChunks)
        {
            VAOModel? model = liquid
                ? chunkToRender.Value.LiquidBlocksModel
                : chunkToRender.Value.HardBlocksModel;

            if (model is null)
            {
                continue;
            }

            var min = new Vector3(chunkToRender.Key.X * 16, 0, chunkToRender.Key.Y * 16);
            Vector3 max = min + new Vector3(16, Constants.MAX_BUILD_HEIGHT, 16);
            if (!_cameraController.Camera.IsAABBInViewFrustum(new AxisAlignedBox(min, max)))
            {
                continue;
            }

            model.BindVAO();
            _basicShader.LoadMatrix(_basicShader.LocationTransformationMatrix, chunkToRender.Value.TransformationMatrix);
            GL.DrawArrays(PrimitiveType.Triangles, 0, model.IndicesCount);
        }
    }

    private void RenderEntities(World world)
    {
        _entityShader.Start();
        _entityShader.LoadMatrix(_entityShader.LocationViewMatrix, _cameraController.Camera.CurrentViewMatrix);

        _entityShader.LoadVector(_entityShader.LocationCameraPosition, _cameraController.Camera.Position);
        _entityShader.LoadVector(_entityShader.LocationFogColor, _fogColor);
        _entityShader.LoadFloat(_entityShader.LocationFogStart, _fogStart);
        _entityShader.LoadFloat(_entityShader.LocationFogEnd, _fogEnd);

        // Every kind of entity wears its own skin, so the bound texture is tracked rather than set once. Mobs
        // of the same kind come out of the collection together often enough for this to be worth it.
        int boundSkinTextureId = -1;

        // The same, for the one uniform that changes per entity rather than per frame. Nothing is being hit
        // on the overwhelming majority of frames, so tracking it means it is uploaded once and left alone.
        float loadedHurtFlash = -1F;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            // Out of the player's own eyes their body would fill the camera, so it is not drawn. Every other
            // view is looking at them from somewhere else, and has to find them there.
            if (entity.ID == _game.ClientPlayer.ID && !IsClientPlayerBodyVisible())
            {
                continue;
            }

            if (!_entityMeshRegistry.Models.TryGetValue(entity.EntityType, out EntityMesh entityMesh))
            {
                continue;
            }

            if (boundSkinTextureId != entityMesh.SkinTextureId)
            {
                _entityShader.LoadTexture(_entityShader.LocationSkinTexture, 0, entityMesh.SkinTextureId);
                boundSkinTextureId = entityMesh.SkinTextureId;
            }

            float hurtFlash = entity is Mob { IsHurt: true } ? HurtFlashStrength : 0F;
            if (loadedHurtFlash != hurtFlash)
            {
                _entityShader.LoadFloat(_entityShader.LocationHurtFlash, hurtFlash);
                loadedHurtFlash = hurtFlash;
            }

            entityMesh.Mesh.BindVAO();
            _entityShader.LoadMatrix(_entityShader.LocationTransformationMatrix, GetEntityTransformation(entity));
            GL.DrawArrays(PrimitiveType.Triangles, 0, entityMesh.Mesh.IndicesCount);
        }
    }

    /// <summary>
    /// Whether the player's own body is drawn: in any view that is not out of their own eyes, which is a
    /// third person perspective or the detached camera looking down at them.
    /// </summary>
    private bool IsClientPlayerBodyVisible()
    {
        return _game.ClientPlayer.IsBodyVisible || _cameraController.Camera != _game.ClientPlayer.Camera;
    }

    /// <summary>
    /// Places an entity's mesh in the world facing its yaw. Entity models are built from a corner rather
    /// than around their middle, so the mesh is walked back onto its own vertical axis before being turned,
    /// otherwise turning would swing it around a corner of its hitbox instead of spinning it on the spot.
    /// </summary>
    private static Matrix4 GetEntityTransformation(Entity entity)
    {
        var pivot = new Vector3(entity.Width / 2.0F, 0, entity.Length / 2.0F);

        return Matrix4.CreateTranslation(-pivot) *
               Matrix4.CreateRotationY(entity.Yaw) *
               Matrix4.CreateTranslation(entity.Position + pivot);
    }

    public void RenderChunkBorders()
    {
        foreach (KeyValuePair<Vector2, Chunk> chunkToRender in _game.World.LoadedChunks)
        {
            var min = new Vector3(chunkToRender.Key.X * 16, 0, chunkToRender.Key.Y * 16);
            _wireframeRenderer.RenderWireframeAt(1, min, new Vector3(16, Constants.MAX_BUILD_HEIGHT, 16), new Vector3(0, 0, 1));
        }

        foreach (KeyValuePair<Vector2, RenderChunk> chunkToRender in _toRenderChunks)
        {
            var min = new Vector3(chunkToRender.Key.X * 16 + 4, 0, chunkToRender.Key.Y * 16 + 4);
            _wireframeRenderer.RenderWireframeAt(1, min, new Vector3(8, Constants.MAX_BUILD_HEIGHT, 8), new Vector3(1, 0, 1));
        }
    }

    private void RunMeshGeneration()
    {
        while (_isRunning)
        {
            Thread.Sleep(5);

            Chunk chunk;
            int generation;
            World world;
            lock (_meshLock)
            {
                // Hold off while a finished mesh is still waiting, since there is only one slot for it.
                // The world is read under the lock as well, since it is taken away on the frame one is left
                // and meshing against nothing would take this thread down with it.
                if (_toRemeshChunksQueue.First is null || _chunkAvailableToRemesh || _game.World is null)
                {
                    continue;
                }

                chunk = _toRemeshChunksQueue.First.Value;
                _toRemeshChunksQueue.RemoveFirst();
                _toRemeshChunksSet.Remove(chunk);
                generation = _worldGeneration;
                world = _game.World;
            }

            ChunkMesh mesh = _blocksMeshGenerator.GenerateMeshFor(world, chunk);

            lock (_meshLock)
            {
                // The world this chunk belongs to may have been left while its mesh was being built, which
                // leaves nothing for it to be drawn as part of.
                if (generation != _worldGeneration)
                {
                    continue;
                }

                _availableChunkMesh = new ChunkRemeshLayout
                {
                    ChunkGridPosition = new Vector2(chunk.GridX, chunk.GridZ),
                    Mesh = mesh,
                };
                _chunkAvailableToRemesh = true;
            }
        }
    }

    public void EndFrameUpdate(float deltaTime)
    {
        RemeshChunkIfMeshAvailable();
        _cameraController.Update();
        _heldItemRenderer.Update(deltaTime);
        _droppedItemRenderer.Update(deltaTime);
    }

    private void RemeshChunkIfMeshAvailable()
    {
        lock (_meshLock)
        {
            if (!_chunkAvailableToRemesh)
            {
                return;
            }

            ChunkRemeshLayout chunkMesh = _availableChunkMesh;
            if (_toRenderChunks.TryGetValue(chunkMesh.ChunkGridPosition, out RenderChunk? renderChunk))
            {
                renderChunk.HardBlocksModel?.CleanUp();
                renderChunk.LiquidBlocksModel?.CleanUp();
            }
            else
            {
                renderChunk = new RenderChunk(
                    (int)chunkMesh.ChunkGridPosition.X,
                    (int)chunkMesh.ChunkGridPosition.Y);
                _toRenderChunks.Add(chunkMesh.ChunkGridPosition, renderChunk);
            }

            renderChunk.HardBlocksModel = new VAOModel(chunkMesh.Mesh.Opaque);

            // A chunk with no water at all still gets asked for its liquid mesh, and an empty buffer would
            // cost a vertex array and a draw call to render nothing.
            renderChunk.LiquidBlocksModel = chunkMesh.Mesh.Liquid.IndicesCount > 0
                ? new VAOModel(chunkMesh.Mesh.Liquid)
                : null;

            _chunkAvailableToRemesh = false;
        }
    }

    public void OnChunkLoaded(World world, Chunk chunk)
    {
        foreach (Chunk editedLightMapChunk in FloodFillLight.GenerateInitialSunlightGrid(world, chunk))
        {
            MeshChunk(editedLightMapChunk);
        }

        foreach (KeyValuePair<Vector3i, BlockState> lightSource in chunk.LightSourceBlocks)
        {
            foreach (Chunk editedLightMapChunk in
                     FloodFillLight.RepairLightGridBlockAdded(world, chunk, lightSource.Key, lightSource.Value))
            {
                MeshChunk(editedLightMapChunk);
            }
        }

        MeshChunk(chunk);
        MeshNeighbourChunks(world, chunk);

        // A neighbouring chunk's light sources can reach into this one, which was not loaded when they were
        // first propagated.
        foreach (Chunk neighbourChunk in world.GetCardinalChunks(chunk))
        {
            foreach (KeyValuePair<Vector3i, BlockState> lightSource in neighbourChunk.LightSourceBlocks)
            {
                foreach (Chunk editedLightMapChunk in
                         FloodFillLight.RepairLightGridBlockAdded(world, neighbourChunk, lightSource.Key, lightSource.Value))
                {
                    MeshChunk(editedLightMapChunk);
                }
            }
        }
    }

    public void OnChunkUnloaded(World world, Chunk chunk)
    {
        var chunkPos = new Vector2(chunk.GridX, chunk.GridZ);

        lock (_meshLock)
        {
            if (_toRemeshChunksSet.Remove(chunk))
            {
                _toRemeshChunksQueue.Remove(chunk);
            }

            if (_chunkAvailableToRemesh && _availableChunkMesh.ChunkGridPosition == chunkPos)
            {
                _chunkAvailableToRemesh = false;
            }
        }

        if (_toRenderChunks.Remove(chunkPos, out RenderChunk? renderChunk))
        {
            renderChunk.CleanUp();
        }

        MeshNeighbourChunks(world, chunk);
    }

    public void OnBlockPlaced(World world, Chunk chunk, Vector3i blockPos, BlockState oldState, BlockState newState)
    {
        MeshChunkAndSurroundings(world, chunk, blockPos, blockRemoved: false, immediate: true);

        foreach (Chunk editedLightMapChunk in FloodFillLight.RepairLightGridBlockAdded(world, chunk, blockPos, newState))
        {
            MeshChunk(editedLightMapChunk, true);
        }

        foreach (Chunk editedLightMapChunk in FloodFillLight.RepairSunlightGridOnBlockAdded(world, chunk, blockPos))
        {
            MeshChunk(editedLightMapChunk, true);
        }
    }

    public void OnBlockRemoved(World world, Chunk chunk, Vector3i blockPos, BlockState oldState)
    {
        foreach (Chunk editedLightMapChunk in FloodFillLight.RepairLightGridBlockRemoved(world, chunk, blockPos))
        {
            MeshChunk(editedLightMapChunk, true);
        }

        foreach (Chunk editedLightMapChunk in FloodFillLight.RepairSunlightGridBlockRemoved(world, chunk, blockPos))
        {
            MeshChunk(editedLightMapChunk, true);
        }

        MeshChunkAndSurroundings(world, chunk, blockPos, blockRemoved: true, immediate: true);
    }

    private void MeshChunk(Chunk chunk, bool immediate = false)
    {
        lock (_meshLock)
        {
            if (!_toRemeshChunksSet.Add(chunk))
            {
                return;
            }

            if (immediate)
            {
                _toRemeshChunksQueue.AddFirst(chunk);
            }
            else
            {
                _toRemeshChunksQueue.AddLast(chunk);
            }
        }
    }

    private void MeshNeighbourChunks(
        World world,
        Chunk chunk,
        bool immediate = false,
        bool meshXNeg = true,
        bool meshXPos = true,
        bool meshZNeg = true,
        bool meshZPos = true)
    {
        if (meshXNeg && world.LoadedChunks.TryGetValue(new Vector2(chunk.GridX - 1, chunk.GridZ), out Chunk? chunkXNeg))
        {
            MeshChunk(chunkXNeg, immediate);
        }

        if (meshXPos && world.LoadedChunks.TryGetValue(new Vector2(chunk.GridX + 1, chunk.GridZ), out Chunk? chunkXPos))
        {
            MeshChunk(chunkXPos, immediate);
        }

        if (meshZNeg && world.LoadedChunks.TryGetValue(new Vector2(chunk.GridX, chunk.GridZ - 1), out Chunk? chunkZNeg))
        {
            MeshChunk(chunkZNeg, immediate);
        }

        if (meshZPos && world.LoadedChunks.TryGetValue(new Vector2(chunk.GridX, chunk.GridZ + 1), out Chunk? chunkZPos))
        {
            MeshChunk(chunkZPos, immediate);
        }
    }

    /// <summary>
    /// Remeshes the chunk a block changed in, plus any neighbour whose own mesh could see the change. Only
    /// blocks on a chunk border can affect a neighbour, which is what the local coordinate tests check.
    /// </summary>
    private void MeshChunkAndSurroundings(World world, Chunk chunk, Vector3i blockPos, bool blockRemoved, bool immediate)
    {
        int localX = blockPos.X & 15;
        int localZ = blockPos.Z & 15;

        bool onXNegBorder = localX == 0;
        bool onXPosBorder = localX == 15;
        bool onZNegBorder = localZ == 0;
        bool onZPosBorder = localZ == 15;

        if (blockRemoved)
        {
            MeshChunk(chunk, immediate);
            MeshNeighbourChunks(world, chunk, immediate, onXNegBorder, onXPosBorder, onZNegBorder, onZPosBorder);
        }
        else
        {
            MeshNeighbourChunks(world, chunk, immediate, onXNegBorder, onXPosBorder, onZNegBorder, onZPosBorder);
            MeshChunk(chunk, immediate);
        }
    }

    private void OnPlayerCameraProjectionChanged(ProjectionMatrixInfo projectionInfo)
    {
        _screenQuad.AdjustToWindowSize(projectionInfo.WindowPixelWidth, projectionInfo.WindowPixelHeight);
        _heldItemRenderer.OnWindowResized(projectionInfo.WindowPixelWidth, projectionInfo.WindowPixelHeight);
        BlockIcons.OnWindowResized(projectionInfo.WindowPixelWidth, projectionInfo.WindowPixelHeight);
        UploadActiveCameraProjectionMatrix();
    }

    private void UploadActiveCameraProjectionMatrix()
    {
        _basicShader.Start();
        _basicShader.LoadMatrix(_basicShader.LocationProjectionMatrix, GetActiveCamera().CurrentProjectionMatrix);
        _entityShader.Start();
        _entityShader.LoadMatrix(_entityShader.LocationProjectionMatrix, GetActiveCamera().CurrentProjectionMatrix);
        _entityShader.Stop();
    }

    public void CleanUp()
    {
        _isRunning = false;

        // The meshing thread touches no GL state, but it does hand over buffers, so let it finish its
        // current chunk before the models it feeds are deleted.
        _meshGenerationThread.Join(TimeSpan.FromSeconds(1));

        _heldItemRenderer.CleanUp();
        _droppedItemRenderer.CleanUp();
        BlockIcons.CleanUp();
        _particleRenderer.CleanUp();
        _basicShader.CleanUp();
        _entityShader.CleanUp();
        _uiRenderer.CleanUp();
        _wireframeRenderer.CleanUp();
        _screenQuad.CleanUp();
        _skydome.CleanUp();
        TextureLoader.Cleanup();

        foreach (KeyValuePair<Vector2, RenderChunk> chunkToRender in _toRenderChunks)
        {
            chunkToRender.Value.CleanUp();
        }

        _toRenderChunks.Clear();
    }

    /// <summary> Enabling depth test ensures that object A behind object B is not rendered over object B. </summary>
    private static void EnableDepthTest()
    {
        GL.Enable(EnableCap.DepthTest);
    }

    private static void EnableCulling()
    {
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
    }
}

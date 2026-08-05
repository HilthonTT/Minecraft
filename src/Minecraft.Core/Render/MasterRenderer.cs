using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Physics;
using Minecraft.Core.Render.Chunks;
using Minecraft.Core.Render.MeshGenerator;
using Minecraft.Core.Render.UI;
using Minecraft.Core.Render.UI.Presets;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shaders.EntityShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
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
        public ChunkBufferLayout ChunkLayout;
    }

    // The colours the framebuffer is cleared with.
    private const float ColorClearR = 0.02F;
    private const float ColorClearG = 0.01F;
    private const float ColorClearB = 0.03F;

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
    private readonly OpaqueMeshGenerator _blocksMeshGenerator;

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

    public DebugHelper DebugHelper { get; }
    public UICanvasIngame IngameCanvas { get; }
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
        _blocksMeshGenerator = new OpaqueMeshGenerator(_blockModelRegistry);
        _entityMeshRegistry = new EntityMeshRegistry();
        _screenQuad = new ScreenQuad(game.Window);
        _wireframeRenderer = new WireframeRenderer(this);
        DebugHelper = new DebugHelper(game, _wireframeRenderer);
        _playerBlockRenderer = new PlayerHoverBlockRenderer(_wireframeRenderer, game.ClientPlayer);
        DitherTextureId = TextureLoader.LoadDitherTexture();
        _skydome = new Skydome(game);

        _uiRenderer = new UIRenderer(_cameraController);
        IngameCanvas = new UICanvasIngame(game);
        AddCanvas(IngameCanvas);

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
        GL.Enable(EnableCap.DepthTest);
        _screenQuad.Bind();
        GL.ClearColor(ColorClearR, ColorClearG, ColorClearB, 1.0F);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _skydome.Render();

        RenderChunks(world);
        RenderEntities(world);

        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        _playerBlockRenderer.RenderSelection();
        DebugHelper.UpdateAndRender();
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _uiRenderer.Render();
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
        _uiRenderer.Render();
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);

        _screenQuad.Unbind();
        _screenQuad.RenderToScreen();
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
        DebugHelper.OnWorldUnloaded();
    }

    /// <summary>
    /// Throws away the mouse movement built up since the cursor was last grabbed. Called when the controls
    /// are handed back to the player, so that closing a menu does not also spin the camera.
    /// </summary>
    public void DiscardPendingMouseLook() => _cameraController.DiscardPendingMouseLook();

    private void RenderChunks(World world)
    {
        _basicShader.Start();
        _basicShader.LoadTexture(_basicShader.LocationTextureAtlas, 0, _textureAtlas.Id);
        _basicShader.LoadMatrix(_basicShader.LocationViewMatrix, _cameraController.Camera.CurrentViewMatrix);
        _basicShader.LoadVector(_basicShader.LocationSunColor, world.Environment.GetCurrentSunColor());
        _basicShader.LoadVector(_basicShader.LocationAmbientColor, world.Environment.AmbientColor);

        foreach (KeyValuePair<Vector2, RenderChunk> chunkToRender in _toRenderChunks)
        {
            VAOModel? model = chunkToRender.Value.HardBlocksModel;
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

        // Every kind of entity wears its own skin, so the bound texture is tracked rather than set once. Mobs
        // of the same kind come out of the collection together often enough for this to be worth it.
        int boundSkinTextureId = -1;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            // The local player's own body would fill the camera, so it is not drawn.
            if (entity.ID == _game.ClientPlayer.ID)
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

            entityMesh.Mesh.BindVAO();
            _entityShader.LoadMatrix(_entityShader.LocationTransformationMatrix, GetEntityTransformation(entity));
            GL.DrawArrays(PrimitiveType.Triangles, 0, entityMesh.Mesh.IndicesCount);
        }
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

            ChunkBufferLayout layout = _blocksMeshGenerator.GenerateMeshFor(world, chunk);

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
                    ChunkLayout = layout,
                };
                _chunkAvailableToRemesh = true;
            }
        }
    }

    public void EndFrameUpdate(World world)
    {
        RemeshChunkIfMeshAvailable();
        _cameraController.Update();
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
            }
            else
            {
                renderChunk = new RenderChunk(
                    (int)chunkMesh.ChunkGridPosition.X,
                    (int)chunkMesh.ChunkGridPosition.Y);
                _toRenderChunks.Add(chunkMesh.ChunkGridPosition, renderChunk);
            }

            renderChunk.HardBlocksModel = new VAOModel(chunkMesh.ChunkLayout);
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

        foreach (Chunk editedLightMapChunk in FloodFillLight.RepairSunlightGridOnBlockAdded(world, chunk, blockPos, newState))
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

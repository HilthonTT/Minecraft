using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Render.Particles;
using Minecraft.Core.Render.UI;
using Minecraft.Core.Render.UI.Presets;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Graphics.OpenGL;

namespace Minecraft.Core.Render;

public sealed class MasterRenderer
{
    private const float ColorClearR = 0.02F;
    private const float ColorClearG = 0.01F;
    private const float ColorClearB = 0.03F;

    private FogState _fog;

    private readonly Game _game;

    private readonly BasicShader _basicShader;
    private readonly CameraController _cameraController;
    private readonly WireframeRenderer _wireframeRenderer;
    private readonly PlayerHoverBlockRenderer _playerBlockRenderer;
    private readonly TextureAtlas _textureAtlas;
    private readonly TextureAtlas _itemAtlas;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly ScreenQuad _screenQuad;
    private readonly UIRenderer _uiRenderer;
    private readonly Skydome _skydome;

    private readonly HeldItemRenderer _heldItemRenderer;
    private readonly DroppedItemRenderer _droppedItemRenderer;
    private readonly EntityRenderer _entityRenderer;
    private readonly ParticleSystem _particleSystem = new();
    private readonly ParticleRenderer _particleRenderer;

    public ChunkRenderer Chunks { get; }

    public ParticleDirector Particles { get; }

    public DebugHelper DebugHelper { get; }
    public UICanvasIngame IngameCanvas { get; }
    public UICanvasHotbar HotbarCanvas { get; }
    public UICanvasInventory InventoryCanvas { get; }

    public ItemIconRenderer ItemIcons { get; }

    public int DitherTextureId { get; }

    public MasterRenderer(Game game)
    {
        _game = game;
        _basicShader = new BasicShader();
        _cameraController = new CameraController(game, game.ClientPlayer.Camera);
        _entityRenderer = new EntityRenderer(game);

        SetActiveCamera(game.ClientPlayer.Camera);

        int textureAtlasId = TextureLoader.LoadBlockAtlas(
            Assets.Path("Resources/texturePack.png"),
            BlockAtlas.CutOutCells,
            BlockAtlas.CellsPerRow);
        _textureAtlas = new TextureAtlas(textureAtlasId, BlockAtlas.SizeInPixels, BlockAtlas.CellSizeInPixels);

        int itemAtlasId = TextureLoader.LoadTexture(Assets.Path("Resources/items.png"));
        _itemAtlas = new TextureAtlas(itemAtlasId, ItemAtlas.SizeInPixels, ItemAtlas.CellSizeInPixels);
        _blockModelRegistry = new BlockModelRegistry(_textureAtlas);
        _screenQuad = new ScreenQuad(game.Window);
        _wireframeRenderer = new WireframeRenderer(this);
        DebugHelper = new DebugHelper(game, _wireframeRenderer);
        _playerBlockRenderer = new PlayerHoverBlockRenderer(_wireframeRenderer, game.ClientPlayer);
        DitherTextureId = TextureLoader.LoadDitherTexture();
        _skydome = new Skydome(game);
        _heldItemRenderer = new HeldItemRenderer(
            game, _basicShader, _blockModelRegistry, _textureAtlas, _itemAtlas);
        _droppedItemRenderer = new DroppedItemRenderer(
            _basicShader, _blockModelRegistry, _textureAtlas, _itemAtlas);
        _particleRenderer = new ParticleRenderer(_basicShader, _textureAtlas);
        Particles = new ParticleDirector(game, _particleSystem, _blockModelRegistry);

        ItemIcons = new ItemIconRenderer(
            _basicShader,
            _blockModelRegistry,
            _textureAtlas,
            _itemAtlas,
            game.Window.ClientSize.X,
            game.Window.ClientSize.Y);

        _uiRenderer = new UIRenderer(_cameraController);
        IngameCanvas = new UICanvasIngame(game);
        AddCanvas(IngameCanvas);

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

        Chunks = new ChunkRenderer(game, _basicShader, _textureAtlas, _blockModelRegistry);
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

        Camera camera = _cameraController.Camera;

        GL.Enable(EnableCap.DepthTest);
        _screenQuad.Bind();

        if (_fog.CameraSubmerged)
        {
            GL.ClearColor(_fog.Color.X, _fog.Color.Y, _fog.Color.Z, 1.0F);
        }
        else
        {
            GL.ClearColor(ColorClearR, ColorClearG, ColorClearB, 1.0F);
        }

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (!_fog.CameraSubmerged)
        {
            _skydome.Render();
        }

        Chunks.RenderSolid(world, camera, _fog);
        _entityRenderer.Render(world, camera, _fog);

        _droppedItemRenderer.Render(world, camera, _fog.Color, _fog.Start, _fog.End);

        _particleRenderer.Render(_particleSystem, camera, world, _fog.Color, _fog.Start, _fog.End);

        Chunks.RenderLiquid(camera);

        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        _playerBlockRenderer.RenderSelection();
        DebugHelper.UpdateAndRender();

        _heldItemRenderer.Render(world, camera);

        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        RenderInterface();
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);

        _screenQuad.Unbind();
        _screenQuad.RenderToScreen();
    }

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

    private void RenderInterface()
    {
        _uiRenderer.Render();

        ItemIcons.Render(_cameraController.Camera);

        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _uiRenderer.RenderOverlays();
    }

    public void UnloadWorld()
    {
        Chunks.UnloadWorld();

        _uiRenderer.RemoveCanvassesIn(RenderSpace.World);

        IngameCanvas.OnWorldUnloaded();
        HotbarCanvas.OnWorldUnloaded();
        DebugHelper.OnWorldUnloaded();
        _heldItemRenderer.OnWorldUnloaded();
        Particles.OnWorldUnloaded();

        ItemIcons.Clear();
    }

    public void DiscardPendingMouseLook() => _cameraController.DiscardPendingMouseLook();

    private float FogStartDistance => _game.Settings.RenderDistanceBlocks * Constants.FOG_START_FRACTION;

    private float FogEndDistance => _game.Settings.RenderDistanceBlocks * Constants.FOG_END_FRACTION;

    private void UpdateFog(World world)
    {
        _fog = FogState.ForCamera(
            world,
            _cameraController.Camera.Position,
            FogStartDistance,
            FogEndDistance);
    }

    public void RenderChunkBorders() => Chunks.RenderBorders(_wireframeRenderer, _game.World);

    public void EndFrameUpdate(float deltaTime)
    {
        Chunks.UploadPendingMesh();
        _cameraController.Update();
        _heldItemRenderer.Update(deltaTime);
        _droppedItemRenderer.Update(deltaTime);
    }

    private void OnPlayerCameraProjectionChanged(ProjectionMatrixInfo projectionInfo)
    {
        _screenQuad.AdjustToWindowSize(projectionInfo.WindowPixelWidth, projectionInfo.WindowPixelHeight);
        _heldItemRenderer.OnWindowResized(projectionInfo.WindowPixelWidth, projectionInfo.WindowPixelHeight);
        ItemIcons.OnWindowResized(projectionInfo.WindowPixelWidth, projectionInfo.WindowPixelHeight);
        UploadActiveCameraProjectionMatrix();
    }

    private void UploadActiveCameraProjectionMatrix()
    {
        _basicShader.Start();
        _basicShader.LoadMatrix(_basicShader.LocationProjectionMatrix, GetActiveCamera().CurrentProjectionMatrix);
        _entityRenderer.UploadProjectionMatrix(GetActiveCamera().CurrentProjectionMatrix);
    }

    public void CleanUp()
    {
        Chunks.CleanUp();

        _heldItemRenderer.CleanUp();
        _droppedItemRenderer.CleanUp();
        ItemIcons.CleanUp();
        _particleRenderer.CleanUp();
        _basicShader.CleanUp();
        _entityRenderer.CleanUp();
        _uiRenderer.CleanUp();
        _wireframeRenderer.CleanUp();
        _screenQuad.CleanUp();
        _skydome.CleanUp();
        TextureLoader.Cleanup();
    }

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

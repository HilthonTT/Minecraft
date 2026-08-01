using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Logging;
using Minecraft.Core.Network;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Physics;
using Minecraft.Core.Render.UI.Presets;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render;

/// <summary>
/// The function key debug tools: hitbox and chunk border overlays, a detached camera, the debug readout and
/// a few world editing shortcuts.
/// </summary>
public sealed class DebugHelper
{
    /// <summary>How far the detached debug camera is placed above the player.</summary>
    private const int DebugCameraHeightAbovePlayer = 100;

    private readonly WireframeRenderer _wireframeRenderer;
    private readonly Game _game;
    private readonly Camera _debugCamera;

    private UICanvasDebug? _debugCanvas;

    private bool _renderFromPlayerCamera = true;
    private bool _renderHitboxes;
    private bool _renderChunkBorders;
    private bool _displayDebugInfo;

    public LightDebugRenderer LightDebug { get; }

    public bool RenderBlockLightAreas { get; private set; }

    public DebugHelper(Game game, WireframeRenderer wireframeRenderer)
    {
        _wireframeRenderer = wireframeRenderer;
        _game = game;
        LightDebug = new LightDebugRenderer(game, wireframeRenderer);

        _debugCamera = new Camera(new ProjectionMatrixInfo
        {
            DistanceNearPlane = 0.1F,
            DistanceFarPlane = 1000F,
            FieldOfView = 1.5F,
            WindowPixelWidth = game.Window.ClientSize.X,
            WindowPixelHeight = game.Window.ClientSize.Y,
        });
    }

    public void UpdateAndRender()
    {
        HandleInput();
        Render();
    }

    private void HandleInput()
    {
        if (Game.Input.OnKeyPress(Keys.F1))
        {
            _renderHitboxes = !_renderHitboxes;
        }
        else if (Game.Input.OnKeyPress(Keys.F2) && _game.RunMode != RunMode.Server)
        {
            ToggleDebugInfo();
        }
        else if (Game.Input.OnKeyPress(Keys.F3))
        {
            GC.Collect();
            Logger.Info("Manual garbage collection.");
        }
        else if (Game.Input.OnKeyPress(Keys.F4))
        {
            ClearBlocksAroundPlayer();
        }
        else if (Game.Input.OnKeyPress(Keys.F5))
        {
            _renderChunkBorders = !_renderChunkBorders;
        }
        else if (Game.Input.OnKeyPress(Keys.F6))
        {
            ToggleDebugCamera();
        }
        else if (Game.Input.OnKeyPress(Keys.F7))
        {
            RenderBlockLightAreas = !RenderBlockLightAreas;
        }
        else if (Game.Input.OnKeyPress(Keys.F8))
        {
            FillChunkLayerWithTnt();
        }
        else if (Game.Input.OnKeyPress(Keys.F9))
        {
            BuildTestRoom();
        }
    }

    private void ToggleDebugInfo()
    {
        _displayDebugInfo = !_displayDebugInfo;

        if (_displayDebugInfo)
        {
            _debugCanvas = new UICanvasDebug(_game);
            _game.MasterRenderer.AddCanvas(_debugCanvas);
        }
        else if (_debugCanvas is not null)
        {
            _game.MasterRenderer.RemoveCanvas(_debugCanvas);
            _debugCanvas = null;
        }
    }

    private void ToggleDebugCamera()
    {
        _renderFromPlayerCamera = !_renderFromPlayerCamera;

        if (_renderFromPlayerCamera)
        {
            _game.MasterRenderer.SetActiveCamera(_game.ClientPlayer.Camera);
            return;
        }

        _debugCamera.SetPosition(_game.ClientPlayer.Position + new Vector3(0, DebugCameraHeightAbovePlayer, 0));

        // Stopped just short of straight down: at exactly ninety degrees the look vector is parallel to the
        // world up axis and the view matrix collapses.
        _debugCamera.SetPitchAndYaw(-MathF.PI / 2.0F + 0.1F, 0);
        _game.MasterRenderer.SetActiveCamera(_debugCamera);
    }

    private void ClearBlocksAroundPlayer()
    {
        const int radius = 3;
        List<Vector3i> blockPositions = [];

        Vector3i playerPos = _game.ClientPlayer.Position.ToBlockPos();
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    blockPositions.Add(new Vector3i(x, y, z) + playerPos);
                }
            }
        }

        _game.Client.WritePacket(new RemoveBlockPacket([.. blockPositions]));
    }

    private void FillChunkLayerWithTnt()
    {
        Vector2 chunkPos = World.GetChunkPosition(_game.ClientPlayer.Position.X, _game.ClientPlayer.Position.Z);
        if (!_game.World.LoadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            return;
        }

        int y = (int)_game.ClientPlayer.Position.Y + 4;
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                _game.Client.WritePacket(new PlaceBlockPacket(
                    BlockRegistry.GetState(BlockRegistry.Tnt),
                    new Vector3i(x + 16 * chunk.GridX, y, z + 16 * chunk.GridZ)));
            }
        }
    }

    /// <summary>Builds a hollow grass box around the player, handy for testing block lighting.</summary>
    private void BuildTestRoom()
    {
        const int size = 8;
        const int height = 7;

        Vector3i origin = _game.ClientPlayer.Position.ToBlockPos();

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                PlaceGrass(origin + new Vector3i(x, 2, z));
                PlaceGrass(origin + new Vector3i(x, 2 + height + 1, z));

                bool isWall = x == 0 || z == 0 || x == size - 1 || z == size - 1;
                if (!isWall)
                {
                    continue;
                }

                for (int y = 1; y <= height; y++)
                {
                    PlaceGrass(origin + new Vector3i(x, 2 + y, z));
                }
            }
        }
    }

    private void PlaceGrass(Vector3i blockPos)
    {
        _game.Client.WritePacket(new PlaceBlockPacket(BlockRegistry.GetState(BlockRegistry.Grass), blockPos));
    }

    private void Render()
    {
        if (_renderHitboxes)
        {
            foreach (Entity entity in _game.World.LoadedEntities.Values)
            {
                AxisAlignedBox aabb = entity.Hitbox;
                var scale = new Vector3(
                    Math.Abs(aabb.Max.X - aabb.Min.X),
                    Math.Abs(aabb.Max.Y - aabb.Min.Y),
                    Math.Abs(aabb.Max.Z - aabb.Min.Z));

                _wireframeRenderer.RenderWireframeAt(2, entity.Position, scale, new Vector3(0.5F, 0, 0));
            }
        }

        if (_renderChunkBorders)
        {
            _game.MasterRenderer.RenderChunkBorders();
        }

        if (RenderBlockLightAreas)
        {
            LightDebug.RenderLightArea();
        }
    }
}

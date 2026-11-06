using System.Diagnostics;
using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Logging;
using Minecraft.Core.Physics;
using Minecraft.Core.Render.MeshGenerator;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shaders.ShadowShader;
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

public sealed class ChunkRenderer
{
    private struct ChunkRemeshLayout
    {
        public Vector2 ChunkGridPosition;
        public ChunkMesh Mesh;
    }

    private const float WaterAlpha = 0.72F;

    private const float SolidAlpha = 1.0F;

    private const int MeshWorkerWaitMilliseconds = 50;

    private const int MaxReadyMeshes = 32;

    private const double UploadBudgetMilliseconds = 4.0;

    private const double NeighbourWaitMilliseconds = 250.0;

    private static readonly Comparison<(float Distance, RenderChunk Chunk)> _nearestFirst =
        (left, right) => left.Distance.CompareTo(right.Distance);

    private static readonly Comparison<(float Distance, RenderChunk Chunk)> _farthestFirst =
        (left, right) => right.Distance.CompareTo(left.Distance);

    private readonly Game _game;
    private readonly BasicShader _shader;
    private readonly TextureAtlas _textureAtlas;
    private readonly ChunkMeshGenerator _meshGenerator;

    private readonly Dictionary<Vector2, RenderChunk> _toRenderChunks = [];
    private readonly List<(float Distance, RenderChunk Chunk)> _drawOrder = [];

    private readonly LinkedList<Chunk> _urgentRemeshQueue = new();
    private readonly List<Chunk> _remeshBacklog = [];
    private readonly HashSet<Chunk> _toRemeshChunksSet = [];
    private readonly Queue<ChunkRemeshLayout> _readyMeshes = new();
    private readonly AutoResetEvent _meshWorkAvailable = new(false);

    private Vector2 _meshFocus;
    private long _lastBacklogPick;

    private int _worldGeneration;

    private Chunk? _meshingChunk;
    private bool _meshingChunkUnloaded;

    private readonly Lock _meshLock = new();
    private readonly Thread _meshGenerationThread;
    private volatile bool _isRunning = true;

    public ChunkRenderer(
        Game game,
        BasicShader shader,
        TextureAtlas textureAtlas,
        BlockModelRegistry blockModelRegistry)
    {
        _game = game;
        _shader = shader;
        _textureAtlas = textureAtlas;
        _meshGenerator = new ChunkMeshGenerator(blockModelRegistry);

        _meshGenerationThread = new Thread(RunMeshGeneration)
        {
            IsBackground = true,
            Name = "Chunk meshing",
        };
        _meshGenerationThread.Start();
    }

    public int TextureAtlasId => _textureAtlas.Id;

    public void RenderSolid(World world, Camera camera, in FogState fog, BasicShader? shaderOverride = null)
    {
        BasicShader shader = shaderOverride ?? _shader;

        shader.Start();
        shader.LoadTexture(shader.LocationTextureAtlas, 0, _textureAtlas.Id);
        shader.LoadMatrix(shader.LocationViewMatrix, camera.CurrentViewMatrix);
        shader.LoadVector(shader.LocationSunColor, world.Environment.GetCurrentSunColor());
        shader.LoadVector(shader.LocationAmbientColor, world.Environment.AmbientColor);

        shader.LoadVector(shader.LocationCameraPosition, camera.Position);
        shader.LoadVector(shader.LocationFogColor, fog.Color);
        shader.LoadFloat(shader.LocationFogStart, fog.Start);
        shader.LoadFloat(shader.LocationFogEnd, fog.End);
        shader.LoadFloat(shader.LocationMaterialAlpha, SolidAlpha);

        DrawChunkModels(shader, camera, liquid: false);
    }

    public void RenderShadowCasters(ShadowShader shader, ShadowMap shadowMap)
    {
        foreach (KeyValuePair<Vector2, RenderChunk> chunkToRender in _toRenderChunks)
        {
            VAOModel? model = chunkToRender.Value.HardBlocksModel;
            if (model is null || model.IndicesCount == 0)
            {
                continue;
            }

            var min = new Vector3(chunkToRender.Key.X * 16, 0, chunkToRender.Key.Y * 16);
            Vector3 max = min + new Vector3(16, Constants.MAX_BUILD_HEIGHT, 16);
            if (!shadowMap.Covers(min, max))
            {
                continue;
            }

            model.BindVAO();
            shader.LoadMatrix(shader.LocationTransformationMatrix, chunkToRender.Value.TransformationMatrix);
            GL.DrawArrays(PrimitiveType.Triangles, 0, model.IndicesCount);
        }

        VAOModel.UnbindVAO();
    }

    public void RenderLiquid(Camera camera, BasicShader? shaderOverride = null)
    {
        BasicShader shader = shaderOverride ?? _shader;

        shader.Start();
        shader.LoadTexture(shader.LocationTextureAtlas, 0, _textureAtlas.Id);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.DepthMask(false);

        GL.Disable(EnableCap.CullFace);

        shader.LoadFloat(shader.LocationMaterialAlpha, WaterAlpha);
        DrawChunkModels(shader, camera, liquid: true);

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    private void DrawChunkModels(BasicShader shader, Camera camera, bool liquid)
    {
        _drawOrder.Clear();

        foreach (KeyValuePair<Vector2, RenderChunk> chunkToRender in _toRenderChunks)
        {
            VAOModel? model = liquid
                ? chunkToRender.Value.LiquidBlocksModel
                : chunkToRender.Value.HardBlocksModel;

            if (model is null || model.IndicesCount == 0)
            {
                continue;
            }

            var min = new Vector3(chunkToRender.Key.X * 16, 0, chunkToRender.Key.Y * 16);
            Vector3 max = min + new Vector3(16, Constants.MAX_BUILD_HEIGHT, 16);
            if (!camera.IsAABBInViewFrustum(new AxisAlignedBox(min, max)))
            {
                continue;
            }

            float offsetX = min.X + 8 - camera.Position.X;
            float offsetZ = min.Z + 8 - camera.Position.Z;
            _drawOrder.Add(((offsetX * offsetX) + (offsetZ * offsetZ), chunkToRender.Value));
        }

        _drawOrder.Sort(liquid ? _farthestFirst : _nearestFirst);

        foreach ((float _, RenderChunk renderChunk) in _drawOrder)
        {
            VAOModel model = liquid ? renderChunk.LiquidBlocksModel! : renderChunk.HardBlocksModel!;

            model.BindVAO();
            shader.LoadMatrix(shader.LocationTransformationMatrix, renderChunk.TransformationMatrix);
            GL.DrawArrays(PrimitiveType.Triangles, 0, model.IndicesCount);
        }
    }

    public void RenderBorders(WireframeRenderer wireframeRenderer, World world)
    {
        foreach (KeyValuePair<Vector2, Chunk> chunkToRender in world.LoadedChunks)
        {
            var min = new Vector3(chunkToRender.Key.X * 16, 0, chunkToRender.Key.Y * 16);
            wireframeRenderer.RenderWireframeAt(1, min, new Vector3(16, Constants.MAX_BUILD_HEIGHT, 16), new Vector3(0, 0, 1));
        }

        foreach (KeyValuePair<Vector2, RenderChunk> chunkToRender in _toRenderChunks)
        {
            var min = new Vector3(chunkToRender.Key.X * 16 + 4, 0, chunkToRender.Key.Y * 16 + 4);
            wireframeRenderer.RenderWireframeAt(1, min, new Vector3(8, Constants.MAX_BUILD_HEIGHT, 8), new Vector3(1, 0, 1));
        }
    }

    private void RunMeshGeneration()
    {
        while (_isRunning)
        {
            Chunk? chunk = null;
            int generation = 0;
            World? world = null;
            Vector2 chunkGridPosition = default;
            lock (_meshLock)
            {
                if (_game.World is not null && _readyMeshes.Count < MaxReadyMeshes)
                {
                    chunk = TakeNextChunkToMesh();
                }

                if (chunk is not null)
                {
                    generation = _worldGeneration;
                    world = _game.World;
                    chunkGridPosition = new Vector2(chunk.GridX, chunk.GridZ);
                    _meshingChunk = chunk;
                    _meshingChunkUnloaded = false;
                }
            }

            if (chunk is null || world is null)
            {
                _meshWorkAvailable.WaitOne(MeshWorkerWaitMilliseconds);
                continue;
            }

            ChunkMesh mesh;
            try
            {
                mesh = _meshGenerator.GenerateMeshFor(world, chunk);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to mesh chunk " + chunkGridPosition + ": " + e);

                lock (_meshLock)
                {
                    _meshingChunk = null;
                }

                continue;
            }

            lock (_meshLock)
            {
                bool unloaded = _meshingChunkUnloaded;
                _meshingChunk = null;

                if (generation != _worldGeneration ||
                    unloaded ||
                    chunk.GridX != (int)chunkGridPosition.X ||
                    chunk.GridZ != (int)chunkGridPosition.Y)
                {
                    continue;
                }

                _readyMeshes.Enqueue(new ChunkRemeshLayout
                {
                    ChunkGridPosition = chunkGridPosition,
                    Mesh = mesh,
                });
            }
        }
    }

    private Chunk? TakeNextChunkToMesh()
    {
        LinkedListNode<Chunk>? urgent = _urgentRemeshQueue.First;
        if (urgent is not null)
        {
            _urgentRemeshQueue.RemoveFirst();
            _toRemeshChunksSet.Remove(urgent.Value);
            return urgent.Value;
        }

        if (_remeshBacklog.Count == 0)
        {
            return null;
        }

        World? world = _game.World;
        int edgeDistance = _game.Settings.RenderDistanceChunks;
        bool waitedLongEnough = Stopwatch.GetElapsedTime(_lastBacklogPick).TotalMilliseconds >= NeighbourWaitMilliseconds;

        int nearest = -1;
        float nearestDistance = float.MaxValue;
        for (int index = 0; index < _remeshBacklog.Count; index++)
        {
            Chunk candidate = _remeshBacklog[index];
            float offsetX = candidate.GridX - _meshFocus.X;
            float offsetZ = candidate.GridZ - _meshFocus.Y;
            float distance = (offsetX * offsetX) + (offsetZ * offsetZ);

            if (distance >= nearestDistance)
            {
                continue;
            }

            bool atEdge = Math.Max(Math.Abs(offsetX), Math.Abs(offsetZ)) >= edgeDistance;
            if (!waitedLongEnough && !atEdge && world is not null && !HasAllNeighbours(world, candidate))
            {
                continue;
            }

            nearestDistance = distance;
            nearest = index;
        }

        if (nearest < 0)
        {
            return null;
        }

        _lastBacklogPick = Stopwatch.GetTimestamp();

        Chunk chunk = _remeshBacklog[nearest];
        _remeshBacklog[nearest] = _remeshBacklog[^1];
        _remeshBacklog.RemoveAt(_remeshBacklog.Count - 1);
        _toRemeshChunksSet.Remove(chunk);
        return chunk;
    }

    private static bool HasAllNeighbours(World world, Chunk chunk)
    {
        return world.LoadedChunks.ContainsKey(new Vector2(chunk.GridX - 1, chunk.GridZ)) &&
               world.LoadedChunks.ContainsKey(new Vector2(chunk.GridX + 1, chunk.GridZ)) &&
               world.LoadedChunks.ContainsKey(new Vector2(chunk.GridX, chunk.GridZ - 1)) &&
               world.LoadedChunks.ContainsKey(new Vector2(chunk.GridX, chunk.GridZ + 1));
    }

    public void UploadPendingMeshes(Vector3 focus)
    {
        long start = Stopwatch.GetTimestamp();
        bool uploaded = false;

        lock (_meshLock)
        {
            _meshFocus = new Vector2(MathF.Floor(focus.X / 16), MathF.Floor(focus.Z / 16));
        }

        while (true)
        {
            ChunkRemeshLayout chunkMesh;
            lock (_meshLock)
            {
                if (!_readyMeshes.TryDequeue(out chunkMesh))
                {
                    break;
                }
            }

            Upload(chunkMesh);
            uploaded = true;

            if (Stopwatch.GetElapsedTime(start).TotalMilliseconds >= UploadBudgetMilliseconds)
            {
                break;
            }
        }

        if (uploaded)
        {
            _meshWorkAvailable.Set();
        }
    }

    private void Upload(ChunkRemeshLayout chunkMesh)
    {
        if (_game.World?.LoadedChunks.ContainsKey(chunkMesh.ChunkGridPosition) != true)
        {
            return;
        }

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

        renderChunk.LiquidBlocksModel = chunkMesh.Mesh.Liquid.IndicesCount > 0
            ? new VAOModel(chunkMesh.Mesh.Liquid)
            : null;
    }

    public void OnChunkLoaded(World world, Chunk chunk)
    {
        foreach (Chunk editedLightMapChunk in SunlightPropagation.GenerateInitialGrid(world, chunk))
        {
            MeshChunk(editedLightMapChunk);
        }

        foreach (Chunk editedLightMapChunk in BlockLightPropagation.LightLoadedChunk(world, chunk))
        {
            MeshChunk(editedLightMapChunk);
        }

        MeshChunk(chunk);
        MeshNeighbourChunks(world, chunk);
    }

    public void OnChunkUnloaded(World world, Chunk chunk)
    {
        var chunkPos = new Vector2(chunk.GridX, chunk.GridZ);

        lock (_meshLock)
        {
            if (_toRemeshChunksSet.Remove(chunk) && !_urgentRemeshQueue.Remove(chunk))
            {
                _remeshBacklog.Remove(chunk);
            }

            for (int remaining = _readyMeshes.Count; remaining > 0; remaining--)
            {
                ChunkRemeshLayout ready = _readyMeshes.Dequeue();
                if (ready.ChunkGridPosition != chunkPos)
                {
                    _readyMeshes.Enqueue(ready);
                }
            }

            if (_meshingChunk == chunk)
            {
                _meshingChunkUnloaded = true;
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

        foreach (Chunk editedLightMapChunk in BlockLightPropagation.RepairOnBlockAdded(world, chunk, blockPos, newState))
        {
            MeshChunk(editedLightMapChunk, true);
        }

        foreach (Chunk editedLightMapChunk in SunlightPropagation.RepairOnBlockAdded(world, chunk, blockPos))
        {
            MeshChunk(editedLightMapChunk, true);
        }
    }

    public void OnBlockStateChanged(World world, Chunk chunk, Vector3i blockPos, BlockState state)
    {
        MeshChunk(chunk, immediate: true);
    }

    public void OnBlockRemoved(World world, Chunk chunk, Vector3i blockPos, BlockState oldState)
    {
        foreach (Chunk editedLightMapChunk in BlockLightPropagation.RepairOnBlockRemoved(world, chunk, blockPos))
        {
            MeshChunk(editedLightMapChunk, true);
        }

        foreach (Chunk editedLightMapChunk in SunlightPropagation.RepairOnBlockRemoved(world, chunk, blockPos))
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
                if (immediate && _remeshBacklog.Remove(chunk))
                {
                    _urgentRemeshQueue.AddLast(chunk);
                }

                return;
            }

            if (immediate)
            {
                _urgentRemeshQueue.AddLast(chunk);
            }
            else
            {
                _remeshBacklog.Add(chunk);
            }
        }

        _meshWorkAvailable.Set();
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

    public void UnloadWorld()
    {
        lock (_meshLock)
        {
            _urgentRemeshQueue.Clear();
            _remeshBacklog.Clear();
            _toRemeshChunksSet.Clear();
            _readyMeshes.Clear();

            _worldGeneration++;
        }

        ClearRenderChunks();
    }

    public void CleanUp()
    {
        _isRunning = false;
        _meshWorkAvailable.Set();

        _meshGenerationThread.Join(TimeSpan.FromSeconds(1));

        ClearRenderChunks();
    }

    private void ClearRenderChunks()
    {
        foreach (KeyValuePair<Vector2, RenderChunk> chunkToRender in _toRenderChunks)
        {
            chunkToRender.Value.CleanUp();
        }

        _toRenderChunks.Clear();
    }
}

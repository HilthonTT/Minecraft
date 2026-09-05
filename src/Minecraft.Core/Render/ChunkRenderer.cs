using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Physics;
using Minecraft.Core.Render.MeshGenerator;
using Minecraft.Core.Shaders.BasicShader;
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

    private const int MeshGenerationIdleMilliseconds = 5;

    private readonly Game _game;
    private readonly BasicShader _shader;
    private readonly TextureAtlas _textureAtlas;
    private readonly ChunkMeshGenerator _meshGenerator;

    private readonly Dictionary<Vector2, RenderChunk> _toRenderChunks = [];

    private readonly LinkedList<Chunk> _toRemeshChunksQueue = new();
    private readonly HashSet<Chunk> _toRemeshChunksSet = [];

    private ChunkRemeshLayout _availableChunkMesh;
    private bool _chunkAvailableToRemesh;

    private int _worldGeneration;

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

    public void RenderSolid(World world, Camera camera, in FogState fog)
    {
        _shader.Start();
        _shader.LoadTexture(_shader.LocationTextureAtlas, 0, _textureAtlas.Id);
        _shader.LoadMatrix(_shader.LocationViewMatrix, camera.CurrentViewMatrix);
        _shader.LoadVector(_shader.LocationSunColor, world.Environment.GetCurrentSunColor());
        _shader.LoadVector(_shader.LocationAmbientColor, world.Environment.AmbientColor);

        _shader.LoadVector(_shader.LocationCameraPosition, camera.Position);
        _shader.LoadVector(_shader.LocationFogColor, fog.Color);
        _shader.LoadFloat(_shader.LocationFogStart, fog.Start);
        _shader.LoadFloat(_shader.LocationFogEnd, fog.End);
        _shader.LoadFloat(_shader.LocationMaterialAlpha, SolidAlpha);

        DrawChunkModels(camera, liquid: false);
    }

    public void RenderLiquid(Camera camera)
    {
        _shader.Start();
        _shader.LoadTexture(_shader.LocationTextureAtlas, 0, _textureAtlas.Id);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.DepthMask(false);

        GL.Disable(EnableCap.CullFace);

        _shader.LoadFloat(_shader.LocationMaterialAlpha, WaterAlpha);
        DrawChunkModels(camera, liquid: true);

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    private void DrawChunkModels(Camera camera, bool liquid)
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
            if (!camera.IsAABBInViewFrustum(new AxisAlignedBox(min, max)))
            {
                continue;
            }

            model.BindVAO();
            _shader.LoadMatrix(_shader.LocationTransformationMatrix, chunkToRender.Value.TransformationMatrix);
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
            Thread.Sleep(MeshGenerationIdleMilliseconds);

            Chunk chunk;
            int generation;
            World world;
            lock (_meshLock)
            {
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

            ChunkMesh mesh = _meshGenerator.GenerateMeshFor(world, chunk);

            lock (_meshLock)
            {
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

    public void UploadPendingMesh()
    {
        lock (_meshLock)
        {
            if (!_chunkAvailableToRemesh)
            {
                return;
            }

            ChunkRemeshLayout chunkMesh = _availableChunkMesh;

            if (_game.World?.LoadedChunks.ContainsKey(chunkMesh.ChunkGridPosition) != true)
            {
                _chunkAvailableToRemesh = false;
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

            _chunkAvailableToRemesh = false;
        }
    }

    public void OnChunkLoaded(World world, Chunk chunk)
    {
        foreach (Chunk editedLightMapChunk in SunlightPropagation.GenerateInitialGrid(world, chunk))
        {
            MeshChunk(editedLightMapChunk);
        }

        foreach (KeyValuePair<Vector3i, BlockState> lightSource in chunk.LightSourceBlocks)
        {
            foreach (Chunk editedLightMapChunk in
                     BlockLightPropagation.RepairOnBlockAdded(world, chunk, lightSource.Key, lightSource.Value))
            {
                MeshChunk(editedLightMapChunk);
            }
        }

        MeshChunk(chunk);
        MeshNeighbourChunks(world, chunk);

        foreach (Chunk neighbourChunk in world.GetCardinalChunks(chunk))
        {
            foreach (KeyValuePair<Vector3i, BlockState> lightSource in neighbourChunk.LightSourceBlocks)
            {
                foreach (Chunk editedLightMapChunk in
                         BlockLightPropagation.RepairOnBlockAdded(world, neighbourChunk, lightSource.Key, lightSource.Value))
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

        foreach (Chunk editedLightMapChunk in BlockLightPropagation.RepairOnBlockAdded(world, chunk, blockPos, newState))
        {
            MeshChunk(editedLightMapChunk, true);
        }

        foreach (Chunk editedLightMapChunk in SunlightPropagation.RepairOnBlockAdded(world, chunk, blockPos))
        {
            MeshChunk(editedLightMapChunk, true);
        }
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
            _toRemeshChunksQueue.Clear();
            _toRemeshChunksSet.Clear();
            _chunkAvailableToRemesh = false;

            _worldGeneration++;
        }

        ClearRenderChunks();
    }

    public void CleanUp()
    {
        _isRunning = false;

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

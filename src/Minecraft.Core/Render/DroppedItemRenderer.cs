using Minecraft.Core.Entities;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Textures;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

/// <summary>
/// Draws the stacks lying on the ground, as small turning blocks that bob where they fell.
/// <para>
/// Not part of the entity pass, which draws skinned models out of the sheets in the resources folder: what an
/// item looks like is the block it is a stack of, so it is drawn out of the block atlas by the same shader
/// the world is, and with the same mesh a slot on the inventory screen uses.
/// </para>
/// </summary>
public sealed class DroppedItemRenderer
{
    /// <summary>How big one is drawn, as a share of a real block.</summary>
    private const float Scale = 0.3F;

    /// <summary>How long it takes to turn once, in seconds.</summary>
    private const float SecondsPerTurn = 4F;

    /// <summary>How far it rises and falls as it turns, in blocks, and how far through a turn that takes.</summary>
    private const float BobHeight = 0.07F;
    private const float BobsPerTurn = 2F;

    private readonly BasicShader _shader;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly TextureAtlas _textureAtlas;

    /// <summary>
    /// One mesh per kind of block, built the first time one of that kind is seen lying about and kept. An
    /// item's own light never changes — it is lit by the sun uniform the way a block icon is — so there is
    /// nothing to rebuild for and a hundred stacks of cobblestone share the one mesh.
    /// </summary>
    private readonly Dictionary<ushort, VAOModel> _meshes = [];

    /// <summary>How long the world has been drawn for, which is all the turning and bobbing are functions of.</summary>
    private float _elapsedSeconds;

    public DroppedItemRenderer(
        BasicShader shader,
        BlockModelRegistry blockModelRegistry,
        TextureAtlas textureAtlas)
    {
        _shader = shader;
        _blockModelRegistry = blockModelRegistry;
        _textureAtlas = textureAtlas;
    }

    public void Update(float deltaTime) => _elapsedSeconds += deltaTime;

    /// <summary>
    /// Draws every item in the world. Spliced in after the entity pass, which leaves its own program bound
    /// and its own skin on texture unit zero, so both are put back before anything is drawn with them.
    /// </summary>
    public void Render(World world, Camera camera, Vector3 fogColor, float fogStart, float fogEnd)
    {
        _shader.Start();
        _shader.LoadTexture(_shader.LocationTextureAtlas, 0, _textureAtlas.Id);
        _shader.LoadMatrix(_shader.LocationViewMatrix, camera.CurrentViewMatrix);
        _shader.LoadVector(_shader.LocationSunColor, world.Environment.GetCurrentSunColor());
        _shader.LoadVector(_shader.LocationAmbientColor, world.Environment.AmbientColor);
        _shader.LoadVector(_shader.LocationCameraPosition, camera.Position);
        _shader.LoadVector(_shader.LocationFogColor, fogColor);
        _shader.LoadFloat(_shader.LocationFogStart, fogStart);
        _shader.LoadFloat(_shader.LocationFogEnd, fogEnd);
        _shader.LoadFloat(_shader.LocationMaterialAlpha, 1.0F);

        float turn = _elapsedSeconds / SecondsPerTurn * MathF.Tau;
        float bob = MathF.Sin(turn * BobsPerTurn) * BobHeight;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is not DroppedItem item || item.Stack.IsEmpty)
            {
                continue;
            }

            VAOModel mesh = MeshFor(item.Stack.Block!);
            mesh.BindVAO();

            // The mesh is built around its own middle, so it is placed at the middle of the body rather than
            // at the corner an entity's position is measured from, or it would turn about one of its edges.
            Vector3 middle = entity.Position + new Vector3(DroppedItem.BodySize / 2F);

            _shader.LoadMatrix(
                _shader.LocationTransformationMatrix,
                Matrix4.CreateScale(Scale) *
                Matrix4.CreateRotationY(turn) *
                Matrix4.CreateTranslation(middle + new Vector3(0F, bob, 0F)));

            GL.DrawArrays(PrimitiveType.Triangles, 0, mesh.IndicesCount);
        }

        VAOModel.UnbindVAO();
    }

    /// <summary>
    /// The mesh for a block, built on first sight. Lit by open daylight, which the sun colour the shader is
    /// given then scales, so an item lying in a field goes dark with the evening. One left on a cave floor
    /// is brighter than its surroundings, which is the price of not rebuilding a mesh per position.
    /// </summary>
    private VAOModel MeshFor(Block block)
    {
        if (_meshes.TryGetValue(block.Id, out VAOModel? mesh))
        {
            return mesh;
        }

        mesh = BlockIconMesh.Build(
            _blockModelRegistry,
            BlockRegistry.GetState(block),
            BlockIconMesh.FullDaylight);

        _meshes.Add(block.Id, mesh);
        return mesh;
    }

    public void CleanUp()
    {
        foreach (VAOModel mesh in _meshes.Values)
        {
            mesh.CleanUp();
        }

        _meshes.Clear();
    }
}

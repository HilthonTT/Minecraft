using Minecraft.Core.Entities;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Textures;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

/// <summary>
/// Draws the stacks lying on the ground, bobbing where they fell.
/// <para>
/// Not part of the entity pass, which draws skinned models out of the sheets in the resources folder: what a
/// dropped stack looks like is what a slot holding it looks like, so it is drawn out of the same two sheets by
/// the same shader the world is, and with the same meshes the inventory screen uses.
/// </para>
/// <para>
/// A block turns on the spot, which is what shows it is a block. A flat sprite cannot: turned edge on it would
/// be a line, and half of every turn would be spent disappearing. So a sprite is kept facing the viewer
/// instead, and what says it is lying there rather than painted on the ground is the bob it shares with the
/// blocks.
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
    private readonly TextureAtlas _itemAtlas;

    /// <summary>
    /// One mesh per kind of item, built the first time one of that kind is seen lying about and kept. An
    /// item's own light never changes — it is lit by the sun uniform the way a block icon is — so there is
    /// nothing to rebuild for and a hundred stacks of cobblestone share the one mesh.
    /// </summary>
    private readonly Dictionary<ushort, VAOModel> _meshes = [];

    /// <summary>How long the world has been drawn for, which is all the turning and bobbing are functions of.</summary>
    private float _elapsedSeconds;

    public DroppedItemRenderer(
        BasicShader shader,
        BlockModelRegistry blockModelRegistry,
        TextureAtlas textureAtlas,
        TextureAtlas itemAtlas)
    {
        _shader = shader;
        _blockModelRegistry = blockModelRegistry;
        _textureAtlas = textureAtlas;
        _itemAtlas = itemAtlas;
    }

    public void Update(float deltaTime) => _elapsedSeconds += deltaTime;

    /// <summary>
    /// Draws every item in the world. Spliced in after the entity pass, which leaves its own program bound
    /// and its own skin on texture unit zero, so both are put back before anything is drawn with them.
    /// </summary>
    public void Render(World world, Camera camera, Vector3 fogColor, float fogStart, float fogEnd)
    {
        _shader.Start();
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

        // Which sheet is bound is tracked rather than uploaded per item, since a quarry floor is usually a
        // great many of one thing.
        int boundAtlas = -1;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is not DroppedItem item || item.Stack.IsEmpty)
            {
                continue;
            }

            Item held = item.Stack.Item!;
            bool isBlock = held is BlockItem;

            int wantedAtlas = isBlock ? _textureAtlas.Id : _itemAtlas.Id;
            if (wantedAtlas != boundAtlas)
            {
                _shader.LoadTexture(_shader.LocationTextureAtlas, 0, wantedAtlas);
                boundAtlas = wantedAtlas;
            }

            VAOModel mesh = MeshFor(held);
            mesh.BindVAO();

            // The mesh is built around its own middle, so it is placed at the middle of the body rather than
            // at the corner an entity's position is measured from, or it would turn about one of its edges.
            Vector3 middle = entity.Position + new Vector3(DroppedItem.BodySize / 2F);
            float yaw = isBlock ? turn : YawTowards(camera.Position, middle);

            _shader.LoadMatrix(
                _shader.LocationTransformationMatrix,
                Matrix4.CreateScale(Scale) *
                Matrix4.CreateRotationY(yaw) *
                Matrix4.CreateTranslation(middle + new Vector3(0F, bob, 0F)));

            GL.DrawArrays(PrimitiveType.Triangles, 0, mesh.IndicesCount);
        }

        VAOModel.UnbindVAO();
    }

    /// <summary>
    /// How far a sprite has to be turned about the upright to face the camera. A flat square is only worth
    /// looking at from the front, so it is given the one angle that shows it.
    /// </summary>
    private static float YawTowards(Vector3 camera, Vector3 item)
    {
        Vector3 toCamera = camera - item;
        return MathF.Atan2(toCamera.X, toCamera.Z);
    }

    /// <summary>
    /// The mesh for one kind of item, built on first sight. Lit by open daylight, which the sun colour the
    /// shader is given then scales, so a stack lying in a field goes dark with the evening. One left on a cave
    /// floor is brighter than its surroundings, which is the price of not rebuilding a mesh per position.
    /// </summary>
    private VAOModel MeshFor(Item item)
    {
        if (_meshes.TryGetValue(item.Id, out VAOModel? mesh))
        {
            return mesh;
        }

        mesh = item switch
        {
            BlockItem block => BlockIconMesh.Build(
                _blockModelRegistry,
                BlockRegistry.GetState(block.Block),
                BlockIconMesh.FullDaylight),
            SpriteItem sprite => ItemSpriteMesh.Build(_itemAtlas, sprite.IconCell),
            _ => throw new ArgumentOutOfRangeException(nameof(item)),
        };

        _meshes.Add(item.Id, mesh);
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

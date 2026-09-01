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

public sealed class DroppedItemRenderer
{
    private const float Scale = 0.3F;

    private const float SecondsPerTurn = 4F;

    private const float BobHeight = 0.07F;
    private const float BobsPerTurn = 2F;

    private readonly BasicShader _shader;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly TextureAtlas _textureAtlas;
    private readonly TextureAtlas _itemAtlas;

    private readonly Dictionary<ushort, VAOModel> _meshes = [];

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

    private static float YawTowards(Vector3 camera, Vector3 item)
    {
        Vector3 toCamera = camera - item;
        return MathF.Atan2(toCamera.X, toCamera.Z);
    }

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

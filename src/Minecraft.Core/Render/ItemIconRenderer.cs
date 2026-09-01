using Minecraft.Core.Entities;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Textures;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public sealed class ItemIconRenderer
{
    private const float IconYaw = 0.785F;
    private const float IconPitch = 0.55F;

    private const float DepthPerIcon = 8F;

    private readonly record struct IconRequest(Item Item, Vector2 Centre, float Size);

    private readonly BasicShader _shader;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly TextureAtlas _textureAtlas;
    private readonly TextureAtlas _itemAtlas;

    private readonly List<IconRequest> _requests = [];

    private readonly Dictionary<ushort, VAOModel> _meshes = [];

    private Matrix4 _projectionMatrix = Matrix4.Identity;

    public ItemIconRenderer(
        BasicShader shader,
        BlockModelRegistry blockModelRegistry,
        TextureAtlas textureAtlas,
        TextureAtlas itemAtlas,
        int pixelWidth,
        int pixelHeight)
    {
        _shader = shader;
        _blockModelRegistry = blockModelRegistry;
        _textureAtlas = textureAtlas;
        _itemAtlas = itemAtlas;

        OnWindowResized(pixelWidth, pixelHeight);
    }

    public void OnWindowResized(int pixelWidth, int pixelHeight)
    {
        _projectionMatrix = Matrix4.CreateOrthographicOffCenter(
            0F,
            Math.Max(1, pixelWidth),
            Math.Max(1, pixelHeight),
            0F,
            -10000F,
            10000F);
    }

    public void Queue(ItemStack stack, Vector2 centre, float size)
    {
        if (stack.IsEmpty || stack.Block == BlockRegistry.Air)
        {
            return;
        }

        _requests.Add(new IconRequest(stack.Item!, centre, size));
    }

    public void Render(Camera activeCamera)
    {
        if (_requests.Count == 0)
        {
            return;
        }

        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);

        GL.Disable(EnableCap.CullFace);

        _shader.Start();
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, _projectionMatrix);
        _shader.LoadMatrix(_shader.LocationViewMatrix, Matrix4.Identity);

        _shader.LoadVector(_shader.LocationSunColor, Vector3.One);
        _shader.LoadVector(_shader.LocationAmbientColor, Vector3.One);
        _shader.LoadFloat(_shader.LocationMaterialAlpha, 1.0F);

        _shader.LoadVector(_shader.LocationCameraPosition, Vector3.Zero);
        _shader.LoadVector(_shader.LocationFogColor, Vector3.Zero);
        _shader.LoadFloat(_shader.LocationFogStart, 100000F);
        _shader.LoadFloat(_shader.LocationFogEnd, 200000F);

        int boundAtlas = -1;

        for (int i = 0; i < _requests.Count; i++)
        {
            IconRequest request = _requests[i];
            bool isBlock = request.Item is BlockItem;

            int wantedAtlas = isBlock ? _textureAtlas.Id : _itemAtlas.Id;
            if (wantedAtlas != boundAtlas)
            {
                _shader.LoadTexture(_shader.LocationTextureAtlas, 0, wantedAtlas);
                boundAtlas = wantedAtlas;
            }

            VAOModel mesh = GetOrBuildMesh(request.Item);

            _shader.LoadMatrix(
                _shader.LocationTransformationMatrix,
                BuildTransformation(request, isBlock, depth: i * DepthPerIcon));

            mesh.BindVAO();
            GL.DrawArrays(PrimitiveType.Triangles, 0, mesh.IndicesCount);
        }

        VAOModel.UnbindVAO();
        _requests.Clear();

        _shader.LoadMatrix(_shader.LocationProjectionMatrix, activeCamera.CurrentProjectionMatrix);

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
    }

    public void Clear() => _requests.Clear();

    private const float TurnedBlockHeight = 1.6F;

    private static Matrix4 BuildTransformation(IconRequest request, bool isBlock, float depth)
    {
        if (!isBlock)
        {
            return Matrix4.CreateScale(request.Size, -request.Size, request.Size)
                   * Matrix4.CreateTranslation(request.Centre.X, request.Centre.Y, depth);
        }

        float scale = request.Size / TurnedBlockHeight;

        return Matrix4.CreateRotationX(IconPitch)
               * Matrix4.CreateRotationY(IconYaw)
               * Matrix4.CreateScale(scale, -scale, scale)
               * Matrix4.CreateTranslation(request.Centre.X, request.Centre.Y, depth);
    }

    private VAOModel GetOrBuildMesh(Item item)
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
        _requests.Clear();
    }
}

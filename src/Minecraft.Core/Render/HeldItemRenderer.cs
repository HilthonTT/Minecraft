using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public sealed class HeldItemRenderer
{
    private const float HandFieldOfView = 1.22F;

    private const float Scale = 0.42F;

    private const float SpriteScale = 0.62F;

    private const float SpriteYaw = 0.62F;
    private const float SpriteRoll = -0.5F;

    private static readonly Vector3 RestingPosition = new(0.58F, -0.46F, -1.05F);

    private const float RestingYaw = 0.72F;
    private const float RestingPitch = -0.22F;

    private const float SwingSeconds = 0.28F;

    private const float SwingDrop = 0.42F;
    private const float SwingTwist = 0.9F;

    private const float BobAmount = 0.022F;
    private const float BobStrideBlocks = 1.4F;

    private const float TeleportDistance = 4F;

    private readonly Game _game;
    private readonly BasicShader _shader;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly TextureAtlas _textureAtlas;
    private readonly TextureAtlas _itemAtlas;

    private VAOModel? _model;

    private Item? _meshedItem;
    private BlockState? _meshedState;
    private uint _meshedLight;

    private bool _meshedIsSprite;

    private Matrix4 _projectionMatrix = Matrix4.Identity;

    private float _swing;

    private float _walkedDistance;
    private Vector3 _lastPlayerPosition;
    private bool _hasLastPosition;

    public HeldItemRenderer(
        Game game,
        BasicShader shader,
        BlockModelRegistry blockModelRegistry,
        TextureAtlas textureAtlas,
        TextureAtlas itemAtlas)
    {
        _game = game;
        _shader = shader;
        _blockModelRegistry = blockModelRegistry;
        _textureAtlas = textureAtlas;
        _itemAtlas = itemAtlas;

        game.ClientPlayer.OnSwingHandler += OnSwing;
        OnWindowResized(game.Window.ClientSize.X, game.Window.ClientSize.Y);
    }

    private void OnSwing() => _swing = 0.0001F;

    public void OnWorldUnloaded()
    {
        _hasLastPosition = false;
        _walkedDistance = 0F;
        _swing = 0F;
    }

    public void OnWindowResized(int pixelWidth, int pixelHeight)
    {
        _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
            HandFieldOfView,
            pixelWidth / (float)Math.Max(1, pixelHeight),
            0.05F,
            10F);
    }

    public void Update(float deltaTime)
    {
        if (_swing > 0F)
        {
            _swing += deltaTime / SwingSeconds;
            if (_swing >= 1F)
            {
                _swing = 0F;
            }
        }

        Vector3 position = _game.ClientPlayer.Position;
        if (_hasLastPosition)
        {
            Vector3 movement = position - _lastPlayerPosition;
            float travelled = new Vector2(movement.X, movement.Z).Length;

            if (travelled <= TeleportDistance)
            {
                _walkedDistance += travelled;
            }
        }

        _lastPlayerPosition = position;
        _hasLastPosition = true;
    }

    public void Render(World world, Camera activeCamera)
    {
        if (activeCamera != _game.ClientPlayer.Camera ||
            _game.ClientPlayer.Perspective != CameraPerspective.FirstPerson)
        {
            return;
        }

        ItemStack held = _game.ClientPlayer.Inventory.Selected;
        if (held.IsEmpty || held.Block == BlockRegistry.Air)
        {
            return;
        }

        RebuildMeshIfStale(world, held);
        if (_model is null)
        {
            return;
        }

        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);

        if (_meshedIsSprite)
        {
            GL.Disable(EnableCap.CullFace);
        }
        else
        {
            GL.Enable(EnableCap.CullFace);
        }

        _shader.Start();
        _shader.LoadTexture(
            _shader.LocationTextureAtlas,
            0,
            _meshedIsSprite ? _itemAtlas.Id : _textureAtlas.Id);
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, _projectionMatrix);

        _shader.LoadMatrix(_shader.LocationViewMatrix, Matrix4.Identity);
        _shader.LoadMatrix(_shader.LocationTransformationMatrix, BuildTransformation());

        _shader.LoadVector(_shader.LocationSunColor, world.Environment.GetCurrentSunColor());
        _shader.LoadVector(_shader.LocationAmbientColor, world.Environment.AmbientColor);
        _shader.LoadFloat(_shader.LocationMaterialAlpha, 1.0F);

        _shader.LoadVector(_shader.LocationCameraPosition, Vector3.Zero);
        _shader.LoadVector(_shader.LocationFogColor, Vector3.Zero);
        _shader.LoadFloat(_shader.LocationFogStart, 1000F);
        _shader.LoadFloat(_shader.LocationFogEnd, 2000F);

        _model.BindVAO();
        GL.DrawArrays(PrimitiveType.Triangles, 0, _model.IndicesCount);
        VAOModel.UnbindVAO();

        _shader.LoadMatrix(_shader.LocationProjectionMatrix, activeCamera.CurrentProjectionMatrix);

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
    }

    private Matrix4 BuildTransformation()
    {
        float swingArc = MathF.Sin(_swing * MathF.PI);

        float bobPhase = _walkedDistance / BobStrideBlocks * MathF.PI;

        var bob = new Vector3(
            MathF.Sin(bobPhase * 2F) * BobAmount,
            -MathF.Abs(MathF.Sin(bobPhase)) * BobAmount,
            0F);

        Vector3 position = RestingPosition + bob + new Vector3(0F, -SwingDrop * swingArc, 0.14F * swingArc);

        if (_meshedIsSprite)
        {
            return Matrix4.CreateScale(SpriteScale)
                   * Matrix4.CreateRotationZ(SpriteRoll)
                   * Matrix4.CreateRotationX(RestingPitch - (SwingTwist * swingArc))
                   * Matrix4.CreateRotationY(SpriteYaw)
                   * Matrix4.CreateTranslation(position);
        }

        return Matrix4.CreateScale(Scale)
               * Matrix4.CreateRotationX(RestingPitch - (SwingTwist * swingArc))
               * Matrix4.CreateRotationY(RestingYaw)
               * Matrix4.CreateTranslation(position);
    }

    private void RebuildMeshIfStale(World world, ItemStack held)
    {
        Light light = SampleLightAtPlayer(world);
        uint packedLight = light.GetStorage();

        if (_model is not null &&
            _meshedItem == held.Item &&
            (_meshedIsSprite || _meshedLight == packedLight))
        {
            return;
        }

        BlockState? state = held.Block is null ? null : BlockRegistry.GetState(held.Block);

        _model?.CleanUp();

        _model = held.Item switch
        {
            SpriteItem sprite => ItemSpriteMesh.Build(_itemAtlas, sprite.IconCell),
            _ => BlockIconMesh.Build(_blockModelRegistry, state!, light),
        };

        _meshedIsSprite = held.Item is SpriteItem;
        _meshedItem = held.Item;
        _meshedState = state;
        _meshedLight = packedLight;
    }

    private Light SampleLightAtPlayer(World world)
    {
        Vector3i blockPos = _game.ClientPlayer.Camera.Position.ToBlockPos();

        if (!world.IsOutsideBuildHeight(blockPos.Y) &&
            world.LoadedChunks.TryGetValue(
                World.GetChunkPosition(blockPos.X, blockPos.Z),
                out Chunk? chunk))
        {
            Vector3i local = blockPos.ToChunkLocal();
            return chunk.LightMap.GetLightColorAt(
                (uint)local.X,
                (uint)local.Y,
                (uint)local.Z,
                BlockIconMesh.LightScale);
        }

        return BlockIconMesh.FullDaylight;
    }

    public void CleanUp()
    {
        _game.ClientPlayer.OnSwingHandler -= OnSwing;
        _model?.CleanUp();
        _model = null;
    }
}

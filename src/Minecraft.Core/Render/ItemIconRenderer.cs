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

/// <summary>
/// Draws whatever is in a slot of the interface, as a small icon of its own.
/// <para>
/// A slot could have shown a flat square of a block's texture, but half the blocks in the game are not
/// squares — a torch, a flower, a cactus — and the ones that are read as the same grey tile as each other
/// until they are turned. So a block icon is the real model, drawn through an orthographic projection laid
/// out in canvas pixels: the same geometry, atlas and shading the world uses, seen from a fixed corner.
/// </para>
/// <para>
/// Everything that is not a block is a picture rather than a shape, and has nothing to gain from being turned
/// at all: it is drawn flat and face on out of the item sheet instead. See <see cref="ItemSpriteMesh"/>. The
/// two kinds share this one pass and are told apart only by which sheet is bound and whether the icon is
/// turned before it is drawn.
/// </para>
/// </summary>
public sealed class ItemIconRenderer
{
    /// <summary>
    /// Where the icon is looked at from. A corner faces the viewer and the block is tipped forward far enough
    /// to show its top, which is what separates grass from dirt at this size.
    /// </summary>
    private const float IconYaw = 0.785F;
    private const float IconPitch = 0.55F;

    /// <summary>
    /// How far apart in depth two icons are placed. The whole pass shares one depth buffer, so a stack drawn
    /// on the cursor over a slot has to be in front of it rather than interleaved with it; anything larger
    /// than an icon's own depth will do, and an icon is at most one block across.
    /// </summary>
    private const float DepthPerIcon = 8F;

    private readonly record struct IconRequest(Item Item, Vector2 Centre, float Size);

    private readonly BasicShader _shader;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly TextureAtlas _textureAtlas;
    private readonly TextureAtlas _itemAtlas;

    /// <summary>What has been asked for this frame, in the order it was asked for, which is also its depth order.</summary>
    private readonly List<IconRequest> _requests = [];

    /// <summary>
    /// One mesh per item, built the first time that item is drawn and kept. Icons are lit by a fixed
    /// daylight rather than by where the player is standing, so a mesh once built never goes stale.
    /// </summary>
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

    /// <summary>
    /// Lays the projection out in canvas pixels with the origin at the top left, so a screen that positions
    /// its slots in pixels can hand those same numbers straight over.
    /// </summary>
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

    /// <summary>
    /// Asks for a stack to be drawn centred on the given point, that many pixels tall. Queued rather than
    /// drawn, since the screens that ask are updated while the interface is being built and the icons
    /// themselves are a pass of their own after it.
    /// <para>
    /// A block is drawn from that block's default state rather than from any state of it: a torch in a slot
    /// stands up whichever wall the one in the world was leaning off.
    /// </para>
    /// </summary>
    public void Queue(ItemStack stack, Vector2 centre, float size)
    {
        if (stack.IsEmpty || stack.Block == BlockRegistry.Air)
        {
            return;
        }

        _requests.Add(new IconRequest(stack.Item!, centre, size));
    }

    /// <summary>
    /// Draws everything queued this frame and empties the queue. Spliced between the interface and the text
    /// over it, both of which are drawn without depth testing, so this puts the depth buffer back to how it
    /// found it and clears it first — nothing behind the icons is ever meant to occlude them.
    /// </summary>
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

        // The vertical flip that puts the origin at the top left also reverses the winding of every face, so
        // culling is off for this pass and the depth test alone decides which side of a block is seen.
        GL.Disable(EnableCap.CullFace);

        _shader.Start();
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, _projectionMatrix);
        _shader.LoadMatrix(_shader.LocationViewMatrix, Matrix4.Identity);

        // Flat daylight with no sky tint, so a slot reads the same at midnight as at noon: what it is for is
        // saying which block this is, not where the sun is.
        _shader.LoadVector(_shader.LocationSunColor, Vector3.One);
        _shader.LoadVector(_shader.LocationAmbientColor, Vector3.One);
        _shader.LoadFloat(_shader.LocationMaterialAlpha, 1.0F);

        _shader.LoadVector(_shader.LocationCameraPosition, Vector3.Zero);
        _shader.LoadVector(_shader.LocationFogColor, Vector3.Zero);
        _shader.LoadFloat(_shader.LocationFogStart, 100000F);
        _shader.LoadFloat(_shader.LocationFogEnd, 200000F);

        // Which sheet is bound is the only state that changes from one icon to the next, so it is tracked
        // rather than uploaded per icon: a screen full of blocks binds once and a mixed one a handful of times.
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

        // Put back what the rest of the frame was drawn with. The world's projection is uploaded only when it
        // changes, so leaving this one bound would flatten the next frame's terrain into the interface.
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, activeCamera.CurrentProjectionMatrix);

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
    }

    /// <summary>Called when a world is left, so nothing queued on the last frame of it is drawn over the menu.</summary>
    public void Clear() => _requests.Clear();

    /// <summary>
    /// How tall a single block comes out once it has been turned onto its corner and tipped forward, as a
    /// multiple of its own width. A block is the tallest thing an icon can be, so dividing the size asked for
    /// by this is what makes that size the height the caller actually gets.
    /// </summary>
    private const float TurnedBlockHeight = 1.6F;

    /// <summary>
    /// Where one icon sits. The scale carries a vertical flip, since the projection has its origin at the top
    /// left and an icon would otherwise be drawn standing on its head.
    /// <para>
    /// A block is turned onto its corner and tipped forward, and so has to be scaled down by how tall that
    /// leaves it. A sprite is drawn square on and already fills exactly the height it was asked for.
    /// </para>
    /// </summary>
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

using Minecraft.Core.Entities;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

/// <summary>
/// Draws blocks as small three dimensional icons in the slots of the interface.
/// <para>
/// A slot could have shown a flat square of the block's texture, but half the blocks in the game are not
/// squares — a torch, a flower, a cactus — and the ones that are read as the same grey tile as each other
/// until they are turned. So the icons are the real models, drawn through an orthographic projection laid out
/// in canvas pixels: the same geometry, atlas and shading the world uses, seen from a fixed corner.
/// </para>
/// </summary>
public sealed class BlockIconRenderer
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

    private readonly record struct IconRequest(Block Block, Vector2 Centre, float Size);

    private readonly BasicShader _shader;
    private readonly BlockModelRegistry _blockModelRegistry;
    private readonly TextureAtlas _textureAtlas;

    /// <summary>What has been asked for this frame, in the order it was asked for, which is also its depth order.</summary>
    private readonly List<IconRequest> _requests = [];

    /// <summary>
    /// One mesh per block, built the first time that block is drawn and kept. Icons are lit by a fixed
    /// daylight rather than by where the player is standing, so a mesh once built never goes stale.
    /// </summary>
    private readonly Dictionary<ushort, VAOModel> _meshes = [];

    private Matrix4 _projectionMatrix = Matrix4.Identity;

    public BlockIconRenderer(
        BasicShader shader,
        BlockModelRegistry blockModelRegistry,
        TextureAtlas textureAtlas,
        int pixelWidth,
        int pixelHeight)
    {
        _shader = shader;
        _blockModelRegistry = blockModelRegistry;
        _textureAtlas = textureAtlas;

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
    /// Asks for a block to be drawn centred on the given point, that many pixels tall. Queued rather than
    /// drawn, since the screens that ask are updated while the interface is being built and the icons
    /// themselves are a pass of their own after it.
    /// <para>
    /// Takes the block rather than a state of it. A slot says which block it holds and nothing more, and an
    /// icon is drawn from the block's default state anyway: a torch in a slot stands up whichever wall the
    /// one in the world was leaning off.
    /// </para>
    /// </summary>
    public void Queue(Block block, Vector2 centre, float size)
    {
        if (block == BlockRegistry.Air)
        {
            return;
        }

        _requests.Add(new IconRequest(block, centre, size));
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
        _shader.LoadTexture(_shader.LocationTextureAtlas, 0, _textureAtlas.Id);
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

        for (int i = 0; i < _requests.Count; i++)
        {
            IconRequest request = _requests[i];
            VAOModel mesh = GetOrBuildMesh(request.Block);

            _shader.LoadMatrix(
                _shader.LocationTransformationMatrix,
                BuildTransformation(request, depth: i * DepthPerIcon));

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
    /// left and a block would otherwise be drawn standing on its head.
    /// </summary>
    private static Matrix4 BuildTransformation(IconRequest request, float depth)
    {
        float scale = request.Size / TurnedBlockHeight;

        return Matrix4.CreateRotationX(IconPitch)
               * Matrix4.CreateRotationY(IconYaw)
               * Matrix4.CreateScale(scale, -scale, scale)
               * Matrix4.CreateTranslation(request.Centre.X, request.Centre.Y, depth);
    }

    private VAOModel GetOrBuildMesh(Block block)
    {
        if (!_meshes.TryGetValue(block.Id, out VAOModel? mesh))
        {
            mesh = BlockIconMesh.Build(
                _blockModelRegistry,
                BlockRegistry.GetState(block),
                BlockIconMesh.FullDaylight);

            _meshes.Add(block.Id, mesh);
        }

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

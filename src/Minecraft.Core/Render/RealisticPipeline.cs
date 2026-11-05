using Minecraft.Core.Entities;
using Minecraft.Core.Shaders.RealisticShader;
using Minecraft.Core.Shaders.ShadowShader;
using Minecraft.Core.Worlds;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public sealed class RealisticPipeline
{
    private const int ShadowMapTextureUnit = 1;

    private const float AnimationPeriodSeconds = 3600F;

    private const float PolygonOffsetFactor = 1.5F;
    private const float PolygonOffsetUnits = 2.0F;

    private readonly ShadowMap _shadowMap = new();
    private readonly ShadowShader _shadowShader = new();

    private float _animationTime;

    public RealisticWorldShader WorldShader { get; } = new();

    public RealisticSkyShader SkyShader { get; } = new();

    public SceneLighting Lighting { get; private set; }

    public float AnimationTime => _animationTime;

    public void Update(float deltaTime)
    {
        _animationTime = (_animationTime + deltaTime) % AnimationPeriodSeconds;
    }

    public void UploadProjectionMatrix(Matrix4 projectionMatrix)
    {
        WorldShader.Start();
        WorldShader.LoadMatrix(WorldShader.LocationProjectionMatrix, projectionMatrix);
        WorldShader.Stop();
    }

    public void Prepare(
        World world,
        Camera camera,
        in FogState fog,
        ChunkRenderer chunks,
        int textureAtlasId,
        int viewportWidth,
        int viewportHeight)
    {
        Lighting = SceneLighting.From(world.Environment);

        if (Lighting.ShadowStrength > 0)
        {
            RenderShadowMap(camera, chunks, textureAtlasId);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, viewportWidth, viewportHeight);
        }

        LoadWorldUniforms(fog);
    }

    private void RenderShadowMap(Camera camera, ChunkRenderer chunks, int textureAtlasId)
    {
        _shadowMap.Update(camera.Position, camera.LookDirection, Lighting.LightDirection);

        _shadowMap.Bind();

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.DepthClamp);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.PolygonOffsetFill);
        GL.PolygonOffset(PolygonOffsetFactor, PolygonOffsetUnits);

        _shadowShader.Start();
        _shadowShader.LoadTexture(_shadowShader.LocationTextureAtlas, 0, textureAtlasId);
        _shadowShader.LoadMatrix(_shadowShader.LocationLightViewProjection, _shadowMap.LightViewProjection);
        _shadowShader.LoadFloat(_shadowShader.LocationAnimationTime, _animationTime);

        chunks.RenderShadowCasters(_shadowShader, _shadowMap);

        _shadowShader.Stop();

        GL.Disable(EnableCap.PolygonOffsetFill);
        GL.Disable(EnableCap.DepthClamp);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
    }

    private void LoadWorldUniforms(in FogState fog)
    {
        RealisticWorldShader shader = WorldShader;
        SceneLighting lighting = Lighting;

        shader.Start();
        shader.LoadTexture(shader.LocationShadowMap, ShadowMapTextureUnit, _shadowMap.DepthTextureId);
        GL.ActiveTexture(TextureUnit.Texture0);

        shader.LoadMatrix(shader.LocationLightViewProjection, _shadowMap.LightViewProjection);
        shader.LoadVector(shader.LocationLightDirection, lighting.LightDirection);
        shader.LoadVector(shader.LocationLightColor, lighting.LightColor);
        shader.LoadVector(shader.LocationSkyAmbientColor, lighting.SkyAmbientColor);
        shader.LoadVector(shader.LocationSkyTopColor, lighting.SkyTopColor);
        shader.LoadVector(shader.LocationHorizonColor, lighting.HorizonColor);
        shader.LoadVector(shader.LocationSunGlowColor, lighting.SunGlowColor);
        shader.LoadVector(shader.LocationSunDirection, lighting.SunDirection);
        shader.LoadFloat(shader.LocationShadowStrength, lighting.ShadowStrength);
        shader.LoadFloat(shader.LocationShadowTexelSize, ShadowMap.TexelSize);
        shader.LoadFloat(shader.LocationAnimationTime, _animationTime);
        shader.LoadFloat(shader.LocationAtmosphere, fog.CameraSubmerged ? 0F : 1F);
    }

    public void CleanUp()
    {
        _shadowMap.CleanUp();
        _shadowShader.CleanUp();
        WorldShader.CleanUp();
        SkyShader.CleanUp();
    }
}

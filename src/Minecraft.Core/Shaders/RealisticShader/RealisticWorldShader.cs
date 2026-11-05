namespace Minecraft.Core.Shaders.RealisticShader;

public sealed class RealisticWorldShader : BasicShader.BasicShader
{
    private const string VertexFile = "Shaders/RealisticShader/vs_realisticWorld.glsl";
    private const string FragmentFile = "Shaders/RealisticShader/fs_realisticWorld.glsl";

    public RealisticWorldShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationShadowMap { get; private set; }

    public int LocationLightViewProjection { get; private set; }

    public int LocationLightDirection { get; private set; }

    public int LocationLightColor { get; private set; }

    public int LocationSkyAmbientColor { get; private set; }

    public int LocationSkyTopColor { get; private set; }

    public int LocationHorizonColor { get; private set; }

    public int LocationSunGlowColor { get; private set; }

    public int LocationSunDirection { get; private set; }

    public int LocationShadowStrength { get; private set; }

    public int LocationShadowTexelSize { get; private set; }

    public int LocationAnimationTime { get; private set; }

    public int LocationAtmosphere { get; private set; }

    protected override void GetAllUniformLocations()
    {
        base.GetAllUniformLocations();

        LocationShadowMap = GetUniformLocation("shadowMap");
        LocationLightViewProjection = GetUniformLocation("lightViewProjection");
        LocationLightDirection = GetUniformLocation("lightDirection");
        LocationLightColor = GetUniformLocation("lightColor");
        LocationSkyAmbientColor = GetUniformLocation("skyAmbientColor");
        LocationSkyTopColor = GetUniformLocation("skyTopColor");
        LocationHorizonColor = GetUniformLocation("horizonColor");
        LocationSunGlowColor = GetUniformLocation("sunGlowColor");
        LocationSunDirection = GetUniformLocation("sunDirection");
        LocationShadowStrength = GetUniformLocation("shadowStrength");
        LocationShadowTexelSize = GetUniformLocation("shadowTexelSize");
        LocationAnimationTime = GetUniformLocation("animationTime");
        LocationAtmosphere = GetUniformLocation("atmosphere");
    }
}

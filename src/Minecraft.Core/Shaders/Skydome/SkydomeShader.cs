namespace Minecraft.Core.Shaders.Skydome;

public sealed class SkydomeShader : Shader
{
    private const string VertexFile = "Shaders/Skydome/vs_skydome.glsl";
    private const string FragmentFile = "Shaders/Skydome/fs_skydome.glsl";

    public SkydomeShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationProjectionMatrix { get; private set; }

    public int LocationSunPosition { get; private set; }

    public int LocationCurrentTime { get; private set; }

    public int LocationTopSkyColor { get; private set; }

    public int LocationBottomSkyColor { get; private set; }

    public int LocationHorizonColor { get; private set; }

    public int LocationSunColor { get; private set; }

    public int LocationSunGlowColor { get; private set; }

    public int LocationMoonColor { get; private set; }

    public int LocationMoonGlowColor { get; private set; }

    public int LocationDitherTexture { get; private set; }

    protected override void GetAllUniformLocations()
    {
        LocationProjectionMatrix = GetUniformLocation("viewProjectionMatrix");
        LocationSunPosition = GetUniformLocation("sunPosition");
        LocationCurrentTime = GetUniformLocation("time");
        LocationTopSkyColor = GetUniformLocation("topSkyColor");
        LocationBottomSkyColor = GetUniformLocation("bottomSkyColor");
        LocationHorizonColor = GetUniformLocation("horizonColor");
        LocationSunColor = GetUniformLocation("sunColor");
        LocationSunGlowColor = GetUniformLocation("sunGlowColor");
        LocationMoonColor = GetUniformLocation("moonColor");
        LocationMoonGlowColor = GetUniformLocation("moonGlowColor");
        LocationDitherTexture = GetUniformLocation("ditherTexture");
    }

    protected override void BindAttributes()
    {
        BindAttribute(0, "vertexPosition");
    }
}

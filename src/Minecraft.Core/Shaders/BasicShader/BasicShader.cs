namespace Minecraft.Core.Shaders.BasicShader;

public sealed class BasicShader : Shader
{
    private const string VertexFile = "Shaders/BasicShader/vertexShader.glsl";
    private const string FragmentFile = "Shaders/BasicShader/fragmentShader.glsl";

    public BasicShader()
    : base(VertexFile, FragmentFile)
    {
    }

    public int LocationTextureAtlas { get; private set; }

    public int LocationTransformationMatrix { get; private set; }

    public int LocationViewMatrix { get; private set; }

    public int LocationProjectionMatrix { get; private set; }

    public int LocationSunColor { get; private set; }

    public int LocationAmbientColor { get; private set; }

    /// <summary>Where the camera is, which the distance the fog is taken over is measured from.</summary>
    public int LocationCameraPosition { get; private set; }

    public int LocationFogColor { get; private set; }

    public int LocationFogStart { get; private set; }

    public int LocationFogEnd { get; private set; }

    protected override void BindAttributes()
    {
        BindAttribute(0, "vertexPosition");
        BindAttribute(1, "vertexNormal");
        BindAttribute(2, "vertexUv");
        BindAttribute(3, "vertexIllumination");
    }

    protected override void GetAllUniformLocations()
    {
        LocationTextureAtlas = GetUniformLocation("textureAtlas");
        LocationTransformationMatrix = GetUniformLocation("transformationMatrix");
        LocationViewMatrix = GetUniformLocation("viewMatrix");
        LocationProjectionMatrix = GetUniformLocation("projectionMatrix");
        LocationSunColor = GetUniformLocation("sunColor");
        LocationAmbientColor = GetUniformLocation("ambientColor");
        LocationCameraPosition = GetUniformLocation("cameraPosition");
        LocationFogColor = GetUniformLocation("fogColor");
        LocationFogStart = GetUniformLocation("fogStart");
        LocationFogEnd = GetUniformLocation("fogEnd");
    }
}

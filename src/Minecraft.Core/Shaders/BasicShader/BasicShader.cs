namespace Minecraft.Core.Shaders.BasicShader;

public sealed class BasicShader : Shader
{
    private const string VertexFile = "../../Shaders/BasicShader/vertexShader.glsl";
    private const string FragmentFile = "../../Shaders/BasicShader/fragmentShader.glsl";

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

    protected override void BindAttributes()
    {
        LocationTextureAtlas = GetUniformLocation("textureAtlas");
        LocationTransformationMatrix = GetUniformLocation("transformationMatrix");
        LocationViewMatrix = GetUniformLocation("viewMatrix");
        LocationProjectionMatrix = GetUniformLocation("projectionMatrix");
        LocationSunColor = GetUniformLocation("sunColor");
        LocationAmbientColor = GetUniformLocation("ambientColor");
    }

    protected override void GetAllUniformLocations()
    {
        BindAttribute(0, "vertexPosition");
        BindAttribute(1, "vertexNormal");
        BindAttribute(2, "vertexUv");
        BindAttribute(3, "vertexIllumination");
    }
}

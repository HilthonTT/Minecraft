namespace Minecraft.Core.Shaders.EntityShader;

public sealed class EntityShader : Shader
{
    private const string VertexFile = "../../Shaders/EntityShader/vs_entityShader.glsl";
    private const string FragmentFile = "../../Shaders/EntityShader/fs_entityShader.glsl";

    public EntityShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationTextureAtlas { get; private set; }

    public int LocationTransformationMatrix { get; private set; }

    public int LocationViewMatrix { get; private set; }

    public int LocationProjectionMatrix { get; private set; }

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
    }
}

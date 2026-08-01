namespace Minecraft.Core.Shaders.WireframeShader;

public sealed class WireframeShader : Shader
{
    private const string VertexFile = "Shaders/WireframeShader/vs_wireframe.glsl";
    private const string FragmentFile = "Shaders/WireframeShader/fs_wireframe.glsl";

    public WireframeShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationTransformationMatrix { get; private set; }

    public int LocationViewMatrix { get; private set; }

    public int LocationProjectionMatrix { get; private set; }

    public int LocationColor { get; private set; }

    protected override void GetAllUniformLocations()
    {
        LocationTransformationMatrix = GetUniformLocation("transformationMatrix");
        LocationViewMatrix = GetUniformLocation("viewMatrix");
        LocationProjectionMatrix = GetUniformLocation("projectionMatrix");
        LocationColor = GetUniformLocation("color");
    }

    protected override void BindAttributes()
    {
        BindAttribute(0, "vertexPosition");
    }
}

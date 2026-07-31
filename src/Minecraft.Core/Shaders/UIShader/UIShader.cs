namespace Minecraft.Core.Shaders.UIShader;

public sealed class UIShader : Shader
{
    private const string VertexFile = "../../Shaders/UIShader/vs_uiShader.glsl";
    private const string FragmentFile = "../../Shaders/UIShader/fs_uiShader.glsl";

    public UIShader() 
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationTexture { get; private set; }

    public int LocationTransformationMatrix { get; private set; }

    public int LocationViewMatrix { get; private set; }

    public int LocationProjectionMatrix { get; private set; }

    public int LocationTransparency { get; private set; }

    public int LocationColor { get; private set; }

    protected override void GetAllUniformLocations()
    {
        LocationTexture = GetUniformLocation("uiTexture");
        LocationTransformationMatrix = GetUniformLocation("transformationMatrix");
        LocationViewMatrix = GetUniformLocation("viewMatrix");
        LocationProjectionMatrix = GetUniformLocation("projectionMatrix");
        LocationTransparency = GetUniformLocation("transparency");
        LocationColor = GetUniformLocation("color");
    }

    protected override void BindAttributes()
    {
        BindAttribute(0, "vertexPosition");
        BindAttribute(1, "vertexUv");
    }
}

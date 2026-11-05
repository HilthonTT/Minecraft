namespace Minecraft.Core.Shaders.RealisticShader;

public sealed class BloomExtractShader : Shader
{
    private const string VertexFile = "Shaders/PostRenderShader/vs_postRender.glsl";
    private const string FragmentFile = "Shaders/RealisticShader/fs_bloomExtract.glsl";

    public BloomExtractShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationSourceTexture { get; private set; }

    public int LocationSourceTexelSize { get; private set; }

    public int LocationThreshold { get; private set; }

    protected override void BindAttributes()
    {
        BindAttribute(0, "vertexPosition");
        BindAttribute(1, "vertexUv");
    }

    protected override void GetAllUniformLocations()
    {
        LocationSourceTexture = GetUniformLocation("sourceTexture");
        LocationSourceTexelSize = GetUniformLocation("sourceTexelSize");
        LocationThreshold = GetUniformLocation("threshold");
    }
}

namespace Minecraft.Core.Shaders.RealisticShader;

public sealed class RealisticPostShader : Shader
{
    private const string VertexFile = "Shaders/PostRenderShader/vs_postRender.glsl";
    private const string FragmentFile = "Shaders/RealisticShader/fs_realisticPost.glsl";

    public RealisticPostShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationColorTexture { get; private set; }

    public int LocationBloomTexture { get; private set; }

    public int LocationExposure { get; private set; }

    public int LocationBloomStrength { get; private set; }

    protected override void BindAttributes()
    {
        BindAttribute(0, "vertexPosition");
        BindAttribute(1, "vertexUv");
    }

    protected override void GetAllUniformLocations()
    {
        LocationColorTexture = GetUniformLocation("colorTexture");
        LocationBloomTexture = GetUniformLocation("bloomTexture");
        LocationExposure = GetUniformLocation("exposure");
        LocationBloomStrength = GetUniformLocation("bloomStrength");
    }
}

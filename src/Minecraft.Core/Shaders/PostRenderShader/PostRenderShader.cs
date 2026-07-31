namespace Minecraft.Core.Shaders.PostRenderShader;

public sealed class PostRenderShader : Shader
{
    private const string VertexFile = "../../Shaders/PostRenderShader/vs_postRender.glsl";
    private const string FragmentFile = "../../Shaders/PostRenderShader/fs_postRender.glsl";

    public PostRenderShader() 
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationColorTexture { get; private set; }

    public int LocationNormalDepthTexture { get; private set; }

    protected override void BindAttributes()
    {
    }

    protected override void GetAllUniformLocations()
    {
        LocationColorTexture = GetUniformLocation("colorTexture");
        LocationNormalDepthTexture = GetUniformLocation("depthNormalTexture");
    }
}

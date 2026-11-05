namespace Minecraft.Core.Shaders.RealisticShader;

public sealed class BloomBlurShader : Shader
{
    private const string VertexFile = "Shaders/PostRenderShader/vs_postRender.glsl";
    private const string FragmentFile = "Shaders/RealisticShader/fs_bloomBlur.glsl";

    public BloomBlurShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationSourceTexture { get; private set; }

    public int LocationBlurStep { get; private set; }

    protected override void BindAttributes()
    {
        BindAttribute(0, "vertexPosition");
        BindAttribute(1, "vertexUv");
    }

    protected override void GetAllUniformLocations()
    {
        LocationSourceTexture = GetUniformLocation("sourceTexture");
        LocationBlurStep = GetUniformLocation("blurStep");
    }
}

namespace Minecraft.Core.Shaders.ShadowShader;

public sealed class ShadowShader : Shader
{
    private const string VertexFile = "Shaders/ShadowShader/vs_shadow.glsl";
    private const string FragmentFile = "Shaders/ShadowShader/fs_shadow.glsl";

    public ShadowShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationTextureAtlas { get; private set; }

    public int LocationTransformationMatrix { get; private set; }

    public int LocationLightViewProjection { get; private set; }

    public int LocationAnimationTime { get; private set; }

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
        LocationLightViewProjection = GetUniformLocation("lightViewProjection");
        LocationAnimationTime = GetUniformLocation("animationTime");
    }
}

namespace Minecraft.Core.Shaders.EntityShader;

public sealed class EntityShader : Shader
{
    private const string VertexFile = "Shaders/EntityShader/vs_entityShader.glsl";
    private const string FragmentFile = "Shaders/EntityShader/fs_entityShader.glsl";

    public EntityShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationSkinTexture { get; private set; }

    public int LocationTransformationMatrix { get; private set; }

    public int LocationViewMatrix { get; private set; }

    public int LocationProjectionMatrix { get; private set; }

    public int LocationCameraPosition { get; private set; }

    public int LocationFogColor { get; private set; }

    public int LocationFogStart { get; private set; }

    public int LocationFogEnd { get; private set; }

    public int LocationHurtFlash { get; private set; }

    protected override void BindAttributes()
    {
        BindAttribute(0, "vertexPosition");
        BindAttribute(1, "vertexNormal");
        BindAttribute(2, "vertexUv");
        BindAttribute(3, "vertexIllumination");
    }

    protected override void GetAllUniformLocations()
    {
        LocationSkinTexture = GetUniformLocation("skinTexture");
        LocationTransformationMatrix = GetUniformLocation("transformationMatrix");
        LocationViewMatrix = GetUniformLocation("viewMatrix");
        LocationProjectionMatrix = GetUniformLocation("projectionMatrix");
        LocationCameraPosition = GetUniformLocation("cameraPosition");
        LocationFogColor = GetUniformLocation("fogColor");
        LocationFogStart = GetUniformLocation("fogStart");
        LocationFogEnd = GetUniformLocation("fogEnd");
        LocationHurtFlash = GetUniformLocation("hurtFlash");
    }
}

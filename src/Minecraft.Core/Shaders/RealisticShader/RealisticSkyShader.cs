using Minecraft.Core.Shaders.Skydome;

namespace Minecraft.Core.Shaders.RealisticShader;

public sealed class RealisticSkyShader : SkydomeShader
{
    private const string VertexFile = "Shaders/Skydome/vs_skydome.glsl";
    private const string FragmentFile = "Shaders/RealisticShader/fs_realisticSky.glsl";

    public RealisticSkyShader()
        : base(VertexFile, FragmentFile)
    {
    }

    public int LocationAnimationTime { get; private set; }

    public int LocationDaylight { get; private set; }

    protected override void GetAllUniformLocations()
    {
        base.GetAllUniformLocations();

        LocationAnimationTime = GetUniformLocation("animationTime");
        LocationDaylight = GetUniformLocation("daylight");
    }
}

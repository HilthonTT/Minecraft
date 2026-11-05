using OpenTK.Mathematics;
using WorldEnvironment = Minecraft.Core.Worlds.Environment;

namespace Minecraft.Core.Render;

public readonly struct SceneLighting
{
    public const float SunPathTilt = 0.42F;

    private const float SunIntensity = 0.85F;
    private const float MoonIntensity = 0.3F;
    private const float MoonShadowStrength = 0.7F;

    private static readonly Vector3 _moonLight = new(0.34F, 0.42F, 0.64F);

    public Vector3 SunDirection { get; init; }

    public Vector3 LightDirection { get; init; }

    public Vector3 LightColor { get; init; }

    public Vector3 SkyAmbientColor { get; init; }

    public Vector3 SkyTopColor { get; init; }

    public Vector3 HorizonColor { get; init; }

    public Vector3 SunGlowColor { get; init; }

    public float ShadowStrength { get; init; }

    public float Daylight { get; init; }

    public static Vector3 TiltSunPath(Vector3 sunPosition)
    {
        (float sin, float cos) = MathF.SinCos(SunPathTilt);
        return new Vector3(sunPosition.X, sunPosition.Y * cos, sunPosition.Y * sin).Normalized();
    }

    public static SceneLighting From(WorldEnvironment environment)
    {
        Vector3 sun = TiltSunPath(environment.SunPosition);
        float sunHeight = sun.Y;

        bool sunUp = sunHeight >= 0;
        Vector3 lightDirection = sunUp ? sun : -sun;
        float lightHeight = MathF.Abs(sunHeight);
        float lightStrength = SmoothStep(-0.02F, 0.25F, lightHeight);

        Vector3 lightColor = sunUp
            ? environment.GetCurrentSunColor() * (SunIntensity * lightStrength)
            : _moonLight * (MoonIntensity * lightStrength);

        float shadowStrength = SmoothStep(0.04F, 0.22F, lightHeight) * (sunUp ? 1.0F : MoonShadowStrength);

        Vector3 top = environment.GetCurrentTopSkyColor();
        Vector3 horizon = environment.GetCurrentHorizonColor();

        return new SceneLighting
        {
            SunDirection = sun,
            LightDirection = lightDirection,
            LightColor = lightColor,
            SkyAmbientColor = SkyAmbientFrom(top, horizon),
            SkyTopColor = top,
            HorizonColor = horizon,
            SunGlowColor = environment.GetCurrentSunGlowColor(),
            ShadowStrength = shadowStrength,
            Daylight = SmoothStep(-0.12F, 0.12F, sunHeight),
        };
    }

    private static Vector3 SkyAmbientFrom(Vector3 top, Vector3 horizon)
    {
        float luma = (0.2126F * top.X) + (0.7152F * top.Y) + (0.0722F * top.Z);
        Vector3 softenedTop = Vector3.Lerp(new Vector3(luma), top, 0.2F);
        return (softenedTop * 0.7F) + (horizon * 0.2F);
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Math.Clamp((value - edge0) / (edge1 - edge0), 0F, 1F);
        return t * t * (3F - (2F * t));
    }
}

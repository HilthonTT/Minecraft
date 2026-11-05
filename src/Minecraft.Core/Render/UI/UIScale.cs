using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public static class UIScale
{
    public const float ReferenceHeight = 720F;
    public const float ReferenceWidth = 1000F;

    public const float MinAutomaticFactor = 0.6F;
    public const float MaxAutomaticFactor = 8F;

    public const int MinPreferencePercent = 50;
    public const int MaxPreferencePercent = 200;

    private static int _windowWidth = 1280;
    private static int _windowHeight = 720;
    private static int _preferencePercent = 100;

    public static float Factor { get; private set; } = 1F;

    public static int CanvasWidth { get; private set; } = 1280;

    public static int CanvasHeight { get; private set; } = 720;

    public static int Version { get; private set; }

    public static void Configure(int windowWidth, int windowHeight, int preferencePercent)
    {
        _windowWidth = Math.Max(1, windowWidth);
        _windowHeight = Math.Max(1, windowHeight);
        _preferencePercent = preferencePercent;
        Recompute();
    }

    public static void SetWindowSize(int windowWidth, int windowHeight)
    {
        Configure(windowWidth, windowHeight, _preferencePercent);
    }

    public static void SetPreference(int preferencePercent)
    {
        if (preferencePercent == _preferencePercent)
        {
            return;
        }

        Configure(_windowWidth, _windowHeight, preferencePercent);
    }

    public static float ComputeFactor(int windowWidth, int windowHeight, int preferencePercent)
    {
        float fit = Math.Min(Math.Max(1, windowHeight) / ReferenceHeight, Math.Max(1, windowWidth) / ReferenceWidth);
        float automatic = Math.Clamp(fit, MinAutomaticFactor, MaxAutomaticFactor);
        float preference = Math.Clamp(preferencePercent, MinPreferencePercent, MaxPreferencePercent) / 100F;
        return automatic * preference;
    }

    public static Vector2i ComputeCanvasSize(int windowWidth, int windowHeight, float factor)
    {
        return new Vector2i(
            Math.Max(1, (int)MathF.Round(windowWidth / factor)),
            Math.Max(1, (int)MathF.Round(windowHeight / factor)));
    }

    public static Vector2 ToCanvas(Vector2 windowPoint)
    {
        return new Vector2(
            windowPoint.X * CanvasWidth / _windowWidth,
            windowPoint.Y * CanvasHeight / _windowHeight);
    }

    public static Vector2 ToWindow(Vector2 canvasPoint)
    {
        return new Vector2(
            canvasPoint.X * _windowWidth / CanvasWidth,
            canvasPoint.Y * _windowHeight / CanvasHeight);
    }

    public static float LengthToWindow(float canvasLength) => canvasLength * _windowHeight / CanvasHeight;

    private static void Recompute()
    {
        Factor = ComputeFactor(_windowWidth, _windowHeight, _preferencePercent);

        Vector2i canvas = ComputeCanvasSize(_windowWidth, _windowHeight, Factor);
        if (canvas.X == CanvasWidth && canvas.Y == CanvasHeight)
        {
            return;
        }

        CanvasWidth = canvas.X;
        CanvasHeight = canvas.Y;
        Version++;
    }
}

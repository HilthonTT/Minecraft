using System.Diagnostics;

namespace Minecraft.Core.Render.UI;

public static class UIMotion
{
    private const float MaxFrameSeconds = 0.1F;

    private static readonly Stopwatch _clock = Stopwatch.StartNew();

    private static double _lastTickSeconds;

    public static float DeltaSeconds { get; private set; }

    public static double NowSeconds => _clock.Elapsed.TotalSeconds;

    public static void Tick()
    {
        double now = NowSeconds;
        DeltaSeconds = (float)Math.Clamp(now - _lastTickSeconds, 0, MaxFrameSeconds);
        _lastTickSeconds = now;
    }

    public static float Approach(float current, float target, float sharpness, float deltaSeconds)
    {
        float blend = 1F - MathF.Exp(-sharpness * Math.Max(0F, deltaSeconds));
        float next = current + ((target - current) * blend);
        return MathF.Abs(target - next) < 0.002F ? target : next;
    }

    public static float Approach(float current, float target, float sharpness)
    {
        return Approach(current, target, sharpness, DeltaSeconds);
    }

    public static float Envelope(float elapsed, float fadeIn, float hold, float fadeOut)
    {
        if (elapsed < 0F)
        {
            return 0F;
        }

        if (elapsed < fadeIn)
        {
            return fadeIn <= 0F ? 1F : elapsed / fadeIn;
        }

        float afterHold = elapsed - fadeIn - hold;
        if (afterHold <= 0F)
        {
            return 1F;
        }

        return fadeOut <= 0F ? 0F : Math.Clamp(1F - (afterHold / fadeOut), 0F, 1F);
    }

    public static float EaseOutCubic(float t)
    {
        float clamped = Math.Clamp(t, 0F, 1F);
        float inverse = 1F - clamped;
        return 1F - (inverse * inverse * inverse);
    }
}

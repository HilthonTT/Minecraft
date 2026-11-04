using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds;

public sealed class Environment
{
    private const float SunriseHour = 6.0F;

    private const float SunsetHour = 18.0F;

    private const float DuskHour = 19.0F;
    private const float DawnHour = 5.0F;

    private float _currentTime;

    public float CurrentTime
    {
        get => _currentTime;
        set => _currentTime = WrapIntoDay(value);
    }

    public int TimeInDay { get; private set; }

    public Vector3 SunPosition { get; set; }

    public Vector3 AmbientColor { get; set; }

    private double _sunRotationRads = 0;

    private readonly Sky _sky;

    public Environment(int timeInDaySeconds)
    {
        if (timeInDaySeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(timeInDaySeconds + " is not valid day length");
        }

        TimeInDay = timeInDaySeconds;
        _sky = new Sky();
    }

    public Vector3 GetCurrentTopSkyColor() => _sky.GetTopSkyColor(GetCurrentEarthTime());
    public Vector3 GetCurrentBottomSkyColor() => _sky.GetBottomSkyColor(GetCurrentEarthTime());
    public Vector3 GetCurrentHorizonColor() => _sky.GetHorizonColor(GetCurrentEarthTime());
    public Vector3 GetCurrentSunColor() => _sky.GetSunColor(GetCurrentEarthTime());
    public Vector3 GetCurrentSunGlowColor() => _sky.GetSunGlowColor(GetCurrentEarthTime());
    public Vector3 GetCurrentMoonColor() => _sky.GetMoonColor(GetCurrentEarthTime());
    public Vector3 GetCurrentMoonGlowColor() => _sky.GetMoonGlowColor(GetCurrentEarthTime());

    public Vector3 GetCurrentFogColor() => _sky.GetHorizonColor(GetCurrentEarthTime());

    private float GetCurrentEarthTime() => CurrentTime * 24.0F / TimeInDay;

    public float CurrentHourOfDay => GetCurrentEarthTime();

    public bool IsNight => CurrentHourOfDay < SunriseHour || CurrentHourOfDay >= SunsetHour;

    public bool IsDarkOutside => CurrentHourOfDay >= DuskHour || CurrentHourOfDay < DawnHour;

    private float WrapIntoDay(float time)
    {
        if (!float.IsFinite(time))
        {
            return 0F;
        }

        float wrapped = time % TimeInDay;
        if (wrapped < 0F)
        {
            wrapped += TimeInDay;
        }

        return wrapped >= TimeInDay ? 0F : wrapped;
    }

    public void Update(float deltaTimeSeconds)
    {
        CurrentTime += deltaTimeSeconds;

        double sunRiseAllignmentOffset = ((TimeInDay / 24.0F * 6.0F) / TimeInDay) * Math.PI * 2;
        _sunRotationRads = (CurrentTime / TimeInDay) * Math.PI * 2 - sunRiseAllignmentOffset;

        Vector3 newSunPosition = new Vector3((float)Math.Cos(_sunRotationRads), (float)Math.Sin(_sunRotationRads), 0).Normalized();
        SunPosition = newSunPosition;
    }
}

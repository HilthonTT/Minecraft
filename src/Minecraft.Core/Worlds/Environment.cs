using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds;

/// <summary>
/// Responsible for updating the different elements that form the environment of the world.
/// Also keeps track of the time.
/// </summary>
public sealed class Environment
{
    /// <summary>The hour the sun comes up, on the 24 hour clock the day is measured against.</summary>
    private const float SunriseHour = 6.0F;

    /// <summary>The hour the sun goes down.</summary>
    private const float SunsetHour = 18.0F;

    /// <summary>
    /// The hour by which the last of the light has gone out of the sky, and the hour at which it starts
    /// coming back. Sunset and sunrise are not the same thing: the sky is still coloured for an hour after
    /// the sun has gone and is already lightening for an hour before it returns, which is why these sit
    /// inside <see cref="IsNight"/> rather than at its edges. See <see cref="IsDarkOutside"/>.
    /// </summary>
    private const float DuskHour = 19.0F;
    private const float DawnHour = 5.0F;

    /// <summary>
    /// The current time in seconds.
    /// </summary>
    public float CurrentTime { get; set; }

    /// <summary>
    /// The total amount of time in an ingame day in seconds.
    /// </summary>
    public int TimeInDay { get; private set; }

    /// <summary>
    /// The current normalized position of the sun on the skydome.
    /// </summary>
    public Vector3 SunPosition { get; set; }

    /// <summary>
    /// The ambient color that is applied to all fragments in the scene.
    /// </summary>
    public Vector3 AmbientColor { get; set; }

    /// <summary>
    /// The position of the sun in terms of rotation around the center of the world/_sky dome.
    /// </summary>
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

    /// <summary>
    /// The colour distant terrain fades into. It is the horizon colour because that is what the terrain is
    /// fading into: the fog closes over at the edge of the loaded world, which sits on the horizon, so
    /// anything else would leave a visible band of the wrong colour where the ground meets the sky. It is
    /// its own method rather than a call to the horizon colour at each use, so that the two can be given
    /// separate colour tables later without hunting down the places that assumed they were the same.
    /// </summary>
    public Vector3 GetCurrentFogColor() => _sky.GetHorizonColor(GetCurrentEarthTime());

    /// <summary>
    /// Returns the current time scaled to earth time, so 24 hour long days
    /// </summary>
    private float GetCurrentEarthTime() => CurrentTime * 24.0F / TimeInDay;

    /// <summary>The hour of a 24 hour day the world is currently at.</summary>
    public float CurrentHourOfDay => GetCurrentEarthTime();

    /// <summary>Whether the sun is below the horizon. It rises at 6 and sets at 18.</summary>
    public bool IsNight => CurrentHourOfDay < SunriseHour || CurrentHourOfDay >= SunsetHour;

    /// <summary>
    /// Whether the open sky is dark, rather than merely sunless. Deliberately a narrower window than
    /// <see cref="IsNight"/>: the hour after sunset and the hour before sunrise are still bright enough to
    /// read the ground by, so anything that comes out in the dark has no business being about in them.
    /// <para>
    /// The gap between the two is what keeps dusk and dawn quiet at both ends. Nothing hostile appears
    /// outdoors from <see cref="DawnHour"/>, an hour before the sun is up, and nothing burns in the open
    /// until it actually is, so a mob that came out in the small hours is neither joined by new ones nor
    /// snuffed out while the sky is still half dark.
    /// </para>
    /// </summary>
    public bool IsDarkOutside => CurrentHourOfDay >= DuskHour || CurrentHourOfDay < DawnHour;

    public void Update(float deltaTimeSeconds)
    {
        CurrentTime += deltaTimeSeconds;
        if (CurrentTime >= TimeInDay)
        {
            CurrentTime = 0;
        }

        //Determine the current position on the unit sphere, add an offset to make angle
        //to make the sun rise at 6AM and set at start setting at 6PM
        double sunRiseAllignmentOffset = ((TimeInDay / 24.0F * 6.0F) / TimeInDay) * Math.PI * 2;
        _sunRotationRads = (CurrentTime / TimeInDay) * Math.PI * 2 - sunRiseAllignmentOffset;

        Vector3 newSunPosition = new Vector3((float)Math.Cos(_sunRotationRads), (float)Math.Sin(_sunRotationRads), 0).Normalized();
        SunPosition = newSunPosition;
    }
}

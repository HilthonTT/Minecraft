using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds;

public sealed class Sky
{
    /*
     * The colors of the different parts of the sky, the sun and moon and other elements can be changed 
     * by altering the color for the given hour. The sky will interpolate between the previous color
     * and the next color linearly for smooth transitions between the different colors.
     */

    private Vector3[] _topSkyColors = new Vector3[24];
    private Vector3[] _bottomSkyColors = new Vector3[24];
    private Vector3[] _horizonColors = new Vector3[24];
    private Vector3[] _sunColors = new Vector3[24];
    private Vector3[] _sunGlowColors = new Vector3[24];
    private Vector3[] _moonColors = new Vector3[24];
    private Vector3[] _moonGlowColors = new Vector3[24];

    private readonly Vector3 _invalidColor = new(-1, -1, -1);

    public Sky()
    {
        for (int i = 0; i < 24; i++)
        {
            _topSkyColors[i] = _invalidColor;
            _bottomSkyColors[i] = _invalidColor;
            _horizonColors[i] = _invalidColor;
            _sunColors[i] = _invalidColor;
            _sunGlowColors[i] = _invalidColor;
            _moonColors[i] = _invalidColor;
            _moonGlowColors[i] = _invalidColor;
        }

        _topSkyColors[4] = new Vector3(0.024F, 0.059F, 0.133F);
        _topSkyColors[6] = new Vector3(0.176F, 0.424F, 0.655F);
        _topSkyColors[8] = new Vector3(0.04F, 0.509F, 0.875F);
        _topSkyColors[16] = new Vector3(0.04F, 0.509F, 0.875F);
        _topSkyColors[18] = new Vector3(0.176F, 0.424F, 0.655F);
        _topSkyColors[20] = new Vector3(0.024F, 0.059F, 0.133F);

        _bottomSkyColors[4] = new Vector3(0.014F, 0.029F, 0.103F);
        _bottomSkyColors[6] = new Vector3(0.517F, 0.686F, 0.949F);
        _bottomSkyColors[8] = new Vector3(0.565F, 0.855F, 0.969F);
        _bottomSkyColors[16] = new Vector3(0.565F, 0.855F, 0.969F);
        _bottomSkyColors[18] = new Vector3(0.517F, 0.686F, 0.949F);
        _bottomSkyColors[20] = new Vector3(0.014F, 0.029F, 0.103F);

        _horizonColors[4] = new Vector3(0.034F, 0.079F, 0.163F);
        _horizonColors[6] = new Vector3(0.696F, 0.349F, 0.231F);
        _horizonColors[8] = new Vector3(0.578F, 0.886F, 1.0F);
        _horizonColors[16] = new Vector3(0.578F, 0.886F, 1.0F);
        _horizonColors[18] = new Vector3(0.696F, 0.349F, 0.231F);
        _horizonColors[20] = new Vector3(0.034F, 0.079F, 0.163F);

        _sunColors[4] = new Vector3(0.034F, 0.079F, 0.163F);
        _sunColors[6] = new Vector3(0.996F, 0.349F, 0.231F);
        _sunColors[8] = new Vector3(1.0F, 1.0F, 0.8F);
        _sunColors[16] = new Vector3(1.0F, 1.0F, 0.8F);
        _sunColors[18] = new Vector3(0.996F, 0.349F, 0.231F);
        _sunColors[20] = new Vector3(0.034F, 0.079F, 0.163F);

        _sunGlowColors[4] = new Vector3(0.030F, 0.069F, 0.133F);
        _sunGlowColors[6] = new Vector3(0.896F, 0.309F, 0.201F);
        _sunGlowColors[8] = new Vector3(0.85F, 0.85F, 0.7F);
        _sunGlowColors[16] = new Vector3(0.85F, 0.85F, 0.7F);
        _sunGlowColors[18] = new Vector3(0.896F, 0.309F, 0.201F);
        _sunGlowColors[20] = new Vector3(0.030F, 0.069F, 0.133F);

        _moonColors[4] = new Vector3(1.0F, 1.0F, 1.0F);
        _moonColors[6] = new Vector3(0.85F, 0.85F, 0.7F);
        _moonColors[8] = new Vector3(1.0F, 1.0F, 1.0F);
        _moonColors[16] = new Vector3(1.0F, 1.0F, 1.0F);
        _moonColors[18] = new Vector3(0.85F, 0.85F, 0.7F);
        _moonColors[20] = new Vector3(1.0F, 1.0F, 1.0F);

        _moonGlowColors[4] = new Vector3(0.224F, 0.259F, 0.233F);
        _moonGlowColors[6] = new Vector3(0.85F, 0.85F, 0.7F);
        _moonGlowColors[8] = new Vector3(1.0F, 1.0F, 1.0F);
        _moonGlowColors[16] = new Vector3(1.0F, 1.0F, 1.0F);
        _moonGlowColors[18] = new Vector3(0.85F, 0.85F, 0.7F);
        _moonGlowColors[20] = new Vector3(0.224F, 0.259F, 0.233F);
    }

    public void SetTopSkyColorTo(Vector3 color, int hour) => _topSkyColors[hour] = color;
    public void SetBottomSkyColorTo(Vector3 color, int hour) => _bottomSkyColors[hour] = color;
    public void SetHorizonColorTo(Vector3 color, int hour) => _horizonColors[hour] = color;
    public void SetSunColorTo(Vector3 color, int hour) => _sunColors[hour] = color;
    public void SetSunGlowColorTo(Vector3 color, int hour) => _sunGlowColors[hour] = color;
    public void SetMoonColorTo(Vector3 color, int hour) => _moonColors[hour] = color;
    public void SetMoonGlowColorTo(Vector3 color, int hour) => _moonGlowColors[hour] = color;

    public Vector3 GetTopSkyColor(float hour) => GetCurrentColorMix(_topSkyColors, hour);
    public Vector3 GetBottomSkyColor(float hour) => GetCurrentColorMix(_bottomSkyColors, hour);
    public Vector3 GetHorizonColor(float hour) => GetCurrentColorMix(_horizonColors, hour);
    public Vector3 GetSunColor(float hour) => GetCurrentColorMix(_sunColors, hour);
    public Vector3 GetSunGlowColor(float hour) => GetCurrentColorMix(_sunGlowColors, hour);
    public Vector3 GetMoonColor(float hour) => GetCurrentColorMix(_moonColors, hour);
    public Vector3 GetMoonGlowColor(float hour) => GetCurrentColorMix(_moonGlowColors, hour);

    private Vector3 GetCurrentColorMix(Vector3[] colors, float hour)
    {
        int prevColorIndex = FindPreviousColorIndex(colors, (int)hour);
        int nextColorIndex = FindNextColorIndex(colors, (int)hour);

        if (prevColorIndex == nextColorIndex)
        {
            return colors[prevColorIndex];
        }

        int hoursTillNextColor = nextColorIndex - prevColorIndex;
        if (hoursTillNextColor < 0)
        {
            hoursTillNextColor += 24;
        }

        int addition = 0;
        if (nextColorIndex < prevColorIndex && hour < nextColorIndex)
        {
            addition = 24;
        }

        float offset = (hour + addition - prevColorIndex) / hoursTillNextColor;
        return colors[prevColorIndex] * (1 - offset) + colors[nextColorIndex] * offset;
    }

    private int FindNextColorIndex(Vector3[] colors, int hour)
    {
        do
        {
            hour++;
            if (hour > 23)
            {
                hour = 0;
            }
        } while (colors[hour] == _invalidColor);

        return hour;
    }

    private int FindPreviousColorIndex(Vector3[] colors, int hour)
    {
        while (colors[hour] == _invalidColor)
        {
            hour--;
            if (hour < 0)
            {
                hour = 23;
            }
        }
        return hour;
    }
}
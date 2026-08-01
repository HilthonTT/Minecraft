namespace Minecraft.Core.Worlds.Lighting;

/// <summary>
/// A packed per vertex light value, uploaded to the shader as a single unsigned integer.
/// <code>
/// 0000 0000 0000 0000 0000 0000 0000 0000
///     II IIII SSSS SSBB BBBB GGGG GGRR RRRR
/// </code>
/// I = illumination, S = sunlight, RGB = the red, green and blue block light channels.
/// </summary>
public struct Light
{
    /// <summary>Every field is six bits wide, so this is the largest value any of them can hold.</summary>
    public const uint MaxChannelValue = 63;

    private const uint ChannelMask = 0x3F;

    private const int RedShift = 0;
    private const int GreenShift = 6;
    private const int BlueShift = 12;
    private const int SunlightShift = 18;
    private const int BrightnessShift = 24;

    private uint _storage;

    public Light(uint red, uint green, uint blue, uint sunlight, uint brightness) : this()
    {
        SetRedChannel(red);
        SetGreenChannel(green);
        SetBlueChannel(blue);
        SetSunlight(sunlight);
        SetBrightness(brightness);
    }

    public readonly uint GetStorage() => _storage;

    public void SetRedChannel(uint value) => SetChannel(value, RedShift, nameof(value));

    public readonly uint GetRedChannel() => (_storage >> RedShift) & ChannelMask;

    public void SetGreenChannel(uint value) => SetChannel(value, GreenShift, nameof(value));

    public readonly uint GetGreenChannel() => (_storage >> GreenShift) & ChannelMask;

    public void SetBlueChannel(uint value) => SetChannel(value, BlueShift, nameof(value));

    public readonly uint GetBlueChannel() => (_storage >> BlueShift) & ChannelMask;

    public void SetSunlight(uint value) => SetChannel(value, SunlightShift, nameof(value));

    public readonly uint GetSunlight() => (_storage >> SunlightShift) & ChannelMask;

    public void SetBrightness(uint value) => SetChannel(value, BrightnessShift, nameof(value));

    public readonly uint GetBrightness() => (_storage >> BrightnessShift) & ChannelMask;

    private void SetChannel(uint value, int shift, string parameterName)
    {
        // Anything wider than six bits would spill into the neighbouring channel rather than be clipped.
        if (value > MaxChannelValue)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"A light channel holds at most {MaxChannelValue}.");
        }

        _storage = (_storage & ~(ChannelMask << shift)) | (value << shift);
    }

    /// <summary>Adds two lights channel by channel, without packing the result back into six bit fields.</summary>
    public static (uint Red, uint Green, uint Blue, uint Sunlight, uint Brightness) Add(Light first, Light second)
    {
        return (
            first.GetRedChannel() + second.GetRedChannel(),
            first.GetGreenChannel() + second.GetGreenChannel(),
            first.GetBlueChannel() + second.GetBlueChannel(),
            first.GetSunlight() + second.GetSunlight(),
            first.GetBrightness() + second.GetBrightness());
    }
}

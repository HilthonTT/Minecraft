using System.Globalization;
using Minecraft.Core.Logging;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Games;

public sealed class GameSettings
{
    private const string FileName = "options.txt";

    public const int MinRenderDistanceChunks = 2;
    public const int MaxRenderDistanceChunks = 16;

    public const float MinFieldOfViewDegrees = 60F;
    public const float MaxFieldOfViewDegrees = 110F;

    public const float MinMouseSensitivity = 0.2F;
    public const float MaxMouseSensitivity = 3.0F;

    public const float DefaultFieldOfViewDegrees = 86F;

    private const string RealisticShadersValue = "realistic";
    private const string NoShadersValue = "off";

    private int _renderDistanceChunks = Constants.VIEW_DISTANCE_CHUNKS;
    private float _fieldOfViewDegrees = DefaultFieldOfViewDegrees;
    private float _masterVolume = 1.0F;
    private float _mouseSensitivity = 1.0F;
    private bool _realisticShaders;

    public event Action? OnChangedHandler;

    public int RenderDistanceChunks
    {
        get => _renderDistanceChunks;
        set => Set(ref _renderDistanceChunks, Math.Clamp(value, MinRenderDistanceChunks, MaxRenderDistanceChunks));
    }

    public float FieldOfViewDegrees
    {
        get => _fieldOfViewDegrees;
        set => Set(ref _fieldOfViewDegrees, Math.Clamp(value, MinFieldOfViewDegrees, MaxFieldOfViewDegrees));
    }

    public float MasterVolume
    {
        get => _masterVolume;
        set => Set(ref _masterVolume, Math.Clamp(value, 0F, 1F));
    }

    public float MouseSensitivity
    {
        get => _mouseSensitivity;
        set => Set(ref _mouseSensitivity, Math.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity));
    }

    public bool RealisticShaders
    {
        get => _realisticShaders;
        set => Set(ref _realisticShaders, value);
    }

    public float FieldOfViewRadians => MathHelper.DegreesToRadians(_fieldOfViewDegrees);

    public float RenderDistanceBlocks => _renderDistanceChunks * 16F;

    public static string DefaultPath => UserData.Path(FileName);

    public static GameSettings Load()
    {
        return Load(DefaultPath);
    }

    public static GameSettings Load(string path)
    {
        var settings = new GameSettings();

        if (!File.Exists(path))
        {
            return settings;
        }

        try
        {
            foreach (string line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                {
                    continue;
                }

                string[] keyValue = trimmed.Split('=', 2);
                if (keyValue.Length != 2)
                {
                    continue;
                }

                settings.Apply(keyValue[0].Trim().ToLowerInvariant(), keyValue[1].Trim());
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Warn("Could not read " + path + ", falling back to the default options: " + e.Message);
        }

        return settings;
    }

    public void Save()
    {
        Save(DefaultPath);
    }

    public void Save(string path)
    {
        try
        {
            File.WriteAllLines(
                path,
                [
                    "renderdistance=" + _renderDistanceChunks.ToString(CultureInfo.InvariantCulture),
                    "fov=" + _fieldOfViewDegrees.ToString("0.#", CultureInfo.InvariantCulture),
                    "volume=" + _masterVolume.ToString("0.###", CultureInfo.InvariantCulture),
                    "sensitivity=" + _mouseSensitivity.ToString("0.###", CultureInfo.InvariantCulture),
                    "shaders=" + (_realisticShaders ? RealisticShadersValue : NoShadersValue),
                ]);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Warn("Could not save the options to " + path + ": " + e.Message);
        }
    }

    public void Apply(string key, string value)
    {
        switch (key)
        {
            case "renderdistance":
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int renderDistance))
                {
                    RenderDistanceChunks = renderDistance;
                }

                break;

            case "fov":
                if (TryParseFloat(value, out float fieldOfView))
                {
                    FieldOfViewDegrees = fieldOfView;
                }

                break;

            case "volume":
                if (TryParseFloat(value, out float volume))
                {
                    MasterVolume = volume;
                }

                break;

            case "sensitivity":
                if (TryParseFloat(value, out float sensitivity))
                {
                    MouseSensitivity = sensitivity;
                }

                break;

            case "shaders":
                if (value.Equals(RealisticShadersValue, StringComparison.OrdinalIgnoreCase))
                {
                    RealisticShaders = true;
                }
                else if (value.Equals(NoShadersValue, StringComparison.OrdinalIgnoreCase))
                {
                    RealisticShaders = false;
                }

                break;
        }
    }

    private static bool TryParseFloat(string value, out float parsed)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && float.IsFinite(parsed);
    }

    private void Set<T>(ref T field, T value)
        where T : IEquatable<T>
    {
        if (field.Equals(value))
        {
            return;
        }

        field = value;
        OnChangedHandler?.Invoke();
    }
}

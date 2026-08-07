using Minecraft.Core.Logging;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;
using System.Globalization;

namespace Minecraft.Core.Games;

/// <summary>
/// What one player has set the game up to look, sound and feel like.
/// <para>
/// Deliberately apart from <see cref="Constants"/>, which describes what the game is. These are preferences
/// about how it is presented, they are read back off disk on the next run, and every one of them takes effect
/// the moment it is changed rather than at the next world.
/// </para>
/// </summary>
public sealed class GameSettings
{
    private const string FileName = "options.txt";

    public const int MinRenderDistanceChunks = 2;
    public const int MaxRenderDistanceChunks = 16;

    public const float MinFieldOfViewDegrees = 60F;
    public const float MaxFieldOfViewDegrees = 110F;

    public const float MinMouseSensitivity = 0.2F;
    public const float MaxMouseSensitivity = 3.0F;

    /// <summary>
    /// The field of view the camera was built with, in degrees. 1.5 radians, which is what every projection
    /// in the game was tuned against, so it is what an untouched installation opens on.
    /// </summary>
    public const float DefaultFieldOfViewDegrees = 86F;

    private int _renderDistanceChunks = Constants.VIEW_DISTANCE_CHUNKS;
    private float _fieldOfViewDegrees = DefaultFieldOfViewDegrees;
    private float _masterVolume = 1.0F;
    private float _mouseSensitivity = 1.0F;

    /// <summary>
    /// Raised after a value has actually changed. Only ever fired for a real change, so a handler that costs
    /// something — telling the server how far this player can see, say — can be hung off it directly even
    /// while a slider is being dragged.
    /// </summary>
    public event Action? OnChangedHandler;

    /// <summary>How far out from the player, in chunks, the world is loaded and drawn.</summary>
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

    /// <summary>How loud everything is, from silent to as recorded.</summary>
    public float MasterVolume
    {
        get => _masterVolume;
        set => Set(ref _masterVolume, Math.Clamp(value, 0F, 1F));
    }

    /// <summary>What the mouse look speed is multiplied by, one being the speed the game was tuned at.</summary>
    public float MouseSensitivity
    {
        get => _mouseSensitivity;
        set => Set(ref _mouseSensitivity, Math.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity));
    }

    /// <summary>The chosen field of view in the radians every projection matrix is built from.</summary>
    public float FieldOfViewRadians => MathHelper.DegreesToRadians(_fieldOfViewDegrees);

    /// <summary>The render distance in blocks, which is what the fog is measured against.</summary>
    public float RenderDistanceBlocks => _renderDistanceChunks * 16F;

    /// <summary>
    /// Reads the saved options, falling back to the defaults for anything missing or unreadable. A settings
    /// file that cannot be parsed is not worth refusing to start over, so it is reported and passed over.
    /// </summary>
    public static GameSettings Load()
    {
        var settings = new GameSettings();
        string path = Assets.Path(FileName);

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

    /// <summary>Writes the options back out. Failing to is a warning, not something worth stopping for.</summary>
    public void Save()
    {
        string path = Assets.Path(FileName);

        try
        {
            File.WriteAllLines(
                path,
                [
                    "renderdistance=" + _renderDistanceChunks.ToString(CultureInfo.InvariantCulture),
                    "fov=" + _fieldOfViewDegrees.ToString("0.#", CultureInfo.InvariantCulture),
                    "volume=" + _masterVolume.ToString("0.###", CultureInfo.InvariantCulture),
                    "sensitivity=" + _mouseSensitivity.ToString("0.###", CultureInfo.InvariantCulture),
                ]);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Warn("Could not save the options to " + path + ": " + e.Message);
        }
    }

    /// <summary>
    /// Takes one line of the settings file. An unreadable value keeps the default rather than throwing: the
    /// worst an edited options file should be able to do is not be honoured.
    /// </summary>
    private void Apply(string key, string value)
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
        }
    }

    private static bool TryParseFloat(string value, out float parsed)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
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

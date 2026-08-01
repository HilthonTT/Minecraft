namespace Minecraft.Core.Worlds.Storage;

/// <summary>
/// The world wide state that is not part of any one chunk. Small enough to be written as plain text, which
/// keeps a save inspectable and lets a seed or time of day be changed by hand.
/// </summary>
public sealed class WorldMetadata
{
    /// <summary>
    /// Bumped whenever the on disk layout changes in a way older saves cannot be read through. A save
    /// carrying a different version is refused rather than misread.
    /// </summary>
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    /// <summary>Seeds the noise fields and the per chunk decoration, and so fixes the terrain.</summary>
    public required int Seed { get; init; }

    /// <summary>Time of day in seconds, carried over so a world reopens at the hour it was left at.</summary>
    public float CurrentTime { get; set; }
}

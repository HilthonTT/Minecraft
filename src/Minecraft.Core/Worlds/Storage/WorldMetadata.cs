using Minecraft.Core.Games;

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
    /// <para>
    /// Version 2 added water, and with it the oceans, rivers and lakes the terrain had to be reshaped to
    /// hold. Only modified chunks are stored and the rest are regenerated, so a version 1 world opened
    /// against this generator would be half its old terrain and half new terrain that no longer joins onto
    /// it. Refusing it outright is the only reading of such a save that is not wrong.
    /// </para>
    /// </summary>
    public const int CurrentVersion = 2;

    public int Version { get; init; } = CurrentVersion;

    /// <summary>Seeds the noise fields and the per chunk decoration, and so fixes the terrain.</summary>
    public required int Seed { get; init; }

    /// <summary>
    /// Time of day in seconds, carried over so a world reopens at the hour it was left at. A world that has
    /// never been saved starts at midday rather than at zero, which would drop the player into the middle
    /// of the night.
    /// </summary>
    public float CurrentTime { get; set; } = World.MiddayTimeSeconds;

    /// <summary>
    /// Which mode the world is played in, chosen when it is created and moved afterwards only by
    /// <c>/gamemode</c>, which writes it back here so a world reopens in the mode it was left in.
    /// <para>
    /// Creative when a save does not name one, which is every world made before there was anything else to
    /// be: an unknown key reads as the fallback rather than as a broken file, so no version bump is needed
    /// and no existing world changes underfoot.
    /// </para>
    /// </summary>
    public GameMode GameMode { get; set; } = GameMode.Creative;
}

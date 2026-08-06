namespace Minecraft.Core.Audio;

/// <summary>
/// The sounds the game raises by name. The two that vary by what a block is made of — walking on it and
/// breaking it — are not here; they are asked for by material through <see cref="SoundRegistry"/> instead.
/// </summary>
public enum Sound
{
    /// <summary>Going into water, whether by walking in or falling in.</summary>
    Splash,

    /// <summary>A stroke while swimming, the water equivalent of a footstep.</summary>
    Swim,

    /// <summary>A lit fuse, played where the TNT that was struck is standing.</summary>
    TntFuse,

    /// <summary>The blast at the end of it.</summary>
    Explode,

    SheepSay,
    SheepStep,
    PigSay,
    PigStep,
    CowSay,
    CowStep,
    ZombieSay,
    ZombieStep,
}

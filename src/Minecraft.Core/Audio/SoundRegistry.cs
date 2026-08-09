using Minecraft.Core.Logging;
using Minecraft.Core.Utilities;

namespace Minecraft.Core.Audio;

/// <summary>
/// Holds every sound the game can play, decoded once at startup.
/// <para>
/// The sound set on disk is far larger than what is used: it carries a mob for every creature Minecraft has
/// and sounds for machinery this game has none of. Only what is named here is read, which is what keeps
/// startup and the memory it costs down to the handful of sets actually reachable.
/// </para>
/// </summary>
public sealed class SoundRegistry
{
    private const string SoundRoot = "Resources/sound-effects";

    /// <summary>Stands in for any set that has not finished loading, and plays nothing.</summary>
    private static readonly SoundGroup _silence = new([]);

    private Dictionary<Sound, SoundGroup>? _sounds;
    private Dictionary<BlockSoundMaterial, SoundGroup>? _steps;
    private Dictionary<BlockSoundMaterial, SoundGroup>? _digs;

    /// <summary>
    /// Starts reading the sound set. Decoding it takes the better part of a second, which is long enough to
    /// be seen as a stall if it were done before the window came up, so it is done off the main thread and
    /// the game is silent for the moment it takes rather than held up by it.
    /// </summary>
    public SoundRegistry()
    {
        Task.Run(Load);
    }

    /// <summary>Whether the set has finished loading. Nothing plays until it has.</summary>
    public bool IsReady => _sounds is not null;

    private void Load()
    {
        long startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        int loaded = 0;

        Dictionary<BlockSoundMaterial, SoundGroup> steps = [];
        Dictionary<BlockSoundMaterial, SoundGroup> digs = [];
        Dictionary<Sound, SoundGroup> sounds = [];

        foreach (BlockSoundMaterial material in Enum.GetValues<BlockSoundMaterial>())
        {
            string setName = FileNameOf(material);
            steps[material] = LoadGroup("step", setName, ref loaded);
            digs[material] = LoadGroup("dig", setName, ref loaded);
        }

        sounds[Sound.Splash] = LoadGroup("liquid", "splash", ref loaded);
        sounds[Sound.Swim] = LoadGroup("liquid", "swim", ref loaded);
        sounds[Sound.TntFuse] = LoadGroup("random", "fuse", ref loaded);
        sounds[Sound.Explode] = LoadGroup("random", "explode", ref loaded);

        sounds[Sound.SheepSay] = LoadGroup("mob/sheep", "say", ref loaded);
        sounds[Sound.SheepStep] = LoadGroup("mob/sheep", "step", ref loaded);
        sounds[Sound.PigSay] = LoadGroup("mob/pig", "say", ref loaded);
        sounds[Sound.PigStep] = LoadGroup("mob/pig", "step", ref loaded);
        sounds[Sound.CowSay] = LoadGroup("mob/cow", "say", ref loaded);
        sounds[Sound.CowStep] = LoadGroup("mob/cow", "step", ref loaded);
        sounds[Sound.ZombieSay] = LoadGroup("mob/zombie", "say", ref loaded);
        sounds[Sound.ZombieStep] = LoadGroup("mob/zombie", "step", ref loaded);

        sounds[Sound.PigDeath] = LoadGroup("mob/pig", "death", ref loaded);
        sounds[Sound.CowHurt] = LoadGroup("mob/cow", "hurt", ref loaded);
        sounds[Sound.ZombieHurt] = LoadGroup("mob/zombie", "hurt", ref loaded);
        sounds[Sound.ZombieDeath] = LoadGroup("mob/zombie", "death", ref loaded);

        // The three are published together and only once every one of them is filled, so a lookup either
        // finds the whole set or finds nothing and stays quiet.
        _steps = steps;
        _digs = digs;
        Volatile.Write(ref _sounds, sounds);

        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startedAt);
        Logger.Info($"Loaded {loaded} sounds in {elapsed.TotalMilliseconds:F0} ms.");
    }

    public SoundGroup Get(Sound sound)
    {
        Dictionary<Sound, SoundGroup>? sounds = Volatile.Read(ref _sounds);
        return sounds is null ? _silence : sounds[sound];
    }

    /// <summary>The sound of walking on a block of this material.</summary>
    public SoundGroup Step(BlockSoundMaterial material)
    {
        return Volatile.Read(ref _sounds) is null ? _silence : _steps![material];
    }

    /// <summary>The sound of a block of this material being broken, which is also what placing one uses.</summary>
    public SoundGroup Dig(BlockSoundMaterial material)
    {
        return Volatile.Read(ref _sounds) is null ? _silence : _digs![material];
    }

    /// <summary>What the set for a material is called on disk, which is Minecraft's own naming.</summary>
    private static string FileNameOf(BlockSoundMaterial material) => material switch
    {
        BlockSoundMaterial.Stone => "stone",
        BlockSoundMaterial.Grass => "grass",
        BlockSoundMaterial.Gravel => "gravel",
        BlockSoundMaterial.Sand => "sand",
        BlockSoundMaterial.Wood => "wood",
        BlockSoundMaterial.Snow => "snow",
        BlockSoundMaterial.Cloth => "cloth",
        _ => "stone",
    };

    /// <summary>
    /// Reads every numbered variant of one sound out of a folder: 'grass' picks up grass1 through grass6,
    /// however many of them there happen to be, so a set can grow or shrink without anything here changing.
    /// </summary>
    private static SoundGroup LoadGroup(string folder, string baseName, ref int loaded)
    {
        string directory = Assets.Path(System.IO.Path.Combine(SoundRoot, folder));
        if (!Directory.Exists(directory))
        {
            Logger.Warn($"Sound folder '{folder}' is missing, '{baseName}' will be silent.");
            return new SoundGroup([]);
        }

        // Ordered so that the set is the same every run, which a directory listing does not promise.
        List<string> files = [.. Directory
            .EnumerateFiles(directory, baseName + "*.ogg")
            .Where(path => IsVariantOf(System.IO.Path.GetFileNameWithoutExtension(path), baseName))
            .Order(StringComparer.Ordinal)];

        if (files.Count == 0)
        {
            Logger.Warn($"No sound files matching '{baseName}' in '{folder}'.");
            return new SoundGroup([]);
        }

        List<SoundClip> clips = [];
        foreach (string file in files)
        {
            SoundClip? clip = SoundLoader.TryLoad(file);
            if (clip is not null)
            {
                clips.Add(clip);
                loaded++;
            }
        }

        return new SoundGroup([.. clips]);
    }

    /// <summary>
    /// Whether a file is one of the numbered variants of a sound rather than a different sound that merely
    /// starts the same way. Without this, asking for the zombie's 'say' would also pick up nothing, but
    /// asking for 'step' in a folder holding 'stepgrass' would quietly take both.
    /// </summary>
    private static bool IsVariantOf(string fileName, string baseName)
    {
        if (!fileName.StartsWith(baseName, StringComparison.Ordinal))
        {
            return false;
        }

        string suffix = fileName[baseName.Length..];
        return suffix.Length == 0 || suffix.All(char.IsAsciiDigit);
    }
}

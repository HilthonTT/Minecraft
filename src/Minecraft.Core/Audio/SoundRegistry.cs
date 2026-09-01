using Minecraft.Core.Logging;
using Minecraft.Core.Utilities;

namespace Minecraft.Core.Audio;

public sealed class SoundRegistry
{
    private const string SoundRoot = "Resources/sound-effects";

    private static readonly SoundGroup _silence = new([]);

    private Dictionary<Sound, SoundGroup>? _sounds;
    private Dictionary<BlockSoundMaterial, SoundGroup>? _steps;
    private Dictionary<BlockSoundMaterial, SoundGroup>? _digs;

    public SoundRegistry()
    {
        Task.Run(Load);
    }

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

        sounds[Sound.PlayerHurt] = LoadGroup("damage", "hit", ref loaded);
        sounds[Sound.ItemPickup] = LoadGroup("random", "pop", ref loaded);
        sounds[Sound.ToolBroke] = LoadGroup("random", "break", ref loaded);

        sounds[Sound.PigDeath] = LoadGroup("mob/pig", "death", ref loaded);
        sounds[Sound.CowHurt] = LoadGroup("mob/cow", "hurt", ref loaded);
        sounds[Sound.ZombieHurt] = LoadGroup("mob/zombie", "hurt", ref loaded);
        sounds[Sound.ZombieDeath] = LoadGroup("mob/zombie", "death", ref loaded);

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

    public SoundGroup Step(BlockSoundMaterial material)
    {
        return Volatile.Read(ref _sounds) is null ? _silence : _steps![material];
    }

    public SoundGroup Dig(BlockSoundMaterial material)
    {
        return Volatile.Read(ref _sounds) is null ? _silence : _digs![material];
    }

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

    private static SoundGroup LoadGroup(string folder, string baseName, ref int loaded)
    {
        string directory = Assets.Path(System.IO.Path.Combine(SoundRoot, folder));
        if (!Directory.Exists(directory))
        {
            Logger.Warn($"Sound folder '{folder}' is missing, '{baseName}' will be silent.");
            return new SoundGroup([]);
        }

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

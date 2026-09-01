using Minecraft.Core.Games;
using Minecraft.Core.Worlds.Storage;

namespace Minecraft.Tests.Worlds;

public sealed class WorldStorageTests : IDisposable
{
    private readonly string _savesRoot =
        Path.Combine(Path.GetTempPath(), "minecraft-tests", Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_savesRoot))
        {
            Directory.Delete(_savesRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("world", "world")]
    [InlineData("  spaced  ", "spaced")]
    [InlineData("a/b", "a_b")]
    [InlineData("a\\b", "a_b")]
    [InlineData("a:b", "a_b")]
    [InlineData("..", "world")]
    [InlineData("../../etc", "_.._etc")]
    [InlineData("", "world")]
    [InlineData("   ", "world")]
    [InlineData("trailing.", "trailing")]
    public void AWorldNameCannotEscapeTheSavesDirectory(string typed, string expected)
    {
        Assert.Equal(expected, WorldSaves.SanitiseWorldName(typed));
    }

    [Fact]
    public void AWorldDirectorySitsInsideTheSavesDirectory()
    {
        string directory = WorldSaves.GetWorldDirectory(_savesRoot, "../escape");

        Assert.Equal(
            Path.GetFullPath(_savesRoot),
            Path.GetFullPath(Path.Combine(directory, "..")));
    }

    [Fact]
    public void NothingIsWrittenUntilSomethingIsSaved()
    {
        using var storage = new WorldStorage(_savesRoot, "fresh");

        WorldMetadata metadata = storage.LoadOrCreateMetadata(seed: 7, GameMode.Survival);

        Assert.Equal(7, metadata.Seed);
        Assert.Equal(GameMode.Survival, metadata.GameMode);
        Assert.Equal(WorldMetadata.CurrentVersion, metadata.Version);
        Assert.False(WorldSaves.WorldExists(_savesRoot, "fresh"));
    }

    [Fact]
    public void AWorldComesBackAsItWasSaved()
    {
        using (var storage = new WorldStorage(_savesRoot, "saved"))
        {
            storage.SaveMetadata(new WorldMetadata
            {
                Seed = -1234,
                CurrentTime = 812.5F,
                GameMode = GameMode.Creative,
            });
        }

        using var reopened = new WorldStorage(_savesRoot, "saved");
        WorldMetadata metadata = reopened.LoadOrCreateMetadata(seed: null, gameMode: null);

        Assert.Equal(-1234, metadata.Seed);
        Assert.Equal(812.5F, metadata.CurrentTime, 3);
        Assert.Equal(GameMode.Creative, metadata.GameMode);
        Assert.True(WorldSaves.WorldExists(_savesRoot, "saved"));
    }

    [Fact]
    public void AnExistingWorldKeepsTheSeedAndModeItWasMadeWith()
    {
        using (var storage = new WorldStorage(_savesRoot, "old"))
        {
            storage.SaveMetadata(new WorldMetadata { Seed = 1, GameMode = GameMode.Survival });
        }

        using var reopened = new WorldStorage(_savesRoot, "old");
        WorldMetadata metadata = reopened.LoadOrCreateMetadata(seed: 999, GameMode.Creative);

        Assert.Equal(1, metadata.Seed);
        Assert.Equal(GameMode.Survival, metadata.GameMode);
    }

    [Fact]
    public void ASaveFromAnotherBuildIsRefusedRatherThanMisread()
    {
        WriteLevelDat("wrong-version", "version=1", "seed=5");

        using var storage = new WorldStorage(_savesRoot, "wrong-version");

        Assert.Throws<InvalidDataException>(() => storage.LoadOrCreateMetadata(null, null));
    }

    [Theory]
    [InlineData("seed=not a number")]
    [InlineData("time=soon")]
    [InlineData("gamemode=spectator")]
    [InlineData("gamemode=7")]
    public void ALevelDatThatSaysSomethingImpossibleIsRefused(string line)
    {
        WriteLevelDat("broken", "version=" + WorldMetadata.CurrentVersion, "seed=5", line);

        using var storage = new WorldStorage(_savesRoot, "broken");

        Assert.Throws<InvalidDataException>(() => storage.LoadOrCreateMetadata(null, null));
    }

    [Fact]
    public void AWorldSavedBeforeThereWasAChoiceOfModeReadsAsCreative()
    {
        WriteLevelDat("modeless", "version=" + WorldMetadata.CurrentVersion, "seed=5");

        using var storage = new WorldStorage(_savesRoot, "modeless");

        Assert.Equal(GameMode.Creative, storage.LoadOrCreateMetadata(null, null).GameMode);
    }

    [Fact]
    public void OnlyDirectoriesHoldingASaveAreListedAsWorlds()
    {
        Save("one");
        Save("two");
        Directory.CreateDirectory(Path.Combine(_savesRoot, "not-a-world"));

        List<string> worlds = WorldSaves.ListWorlds(_savesRoot);

        Assert.Equal(2, worlds.Count);
        Assert.Contains("one", worlds);
        Assert.Contains("two", worlds);
    }

    [Fact]
    public void ThereAreNoWorldsBeforeAnyHaveBeenSaved()
    {
        Assert.Empty(WorldSaves.ListWorlds(_savesRoot));
    }

    [Fact]
    public void ANameAlreadyInUseIsOfferedWithANumberOnIt()
    {
        Save("world");

        Assert.Equal("world2", WorldSaves.SuggestUnusedWorldName(_savesRoot, "world"));
        Assert.Equal("other", WorldSaves.SuggestUnusedWorldName(_savesRoot, "other"));
    }

    [Fact]
    public void RenamingMovesTheWholeWorld()
    {
        Save("before");

        Assert.Equal(WorldRenameResult.Renamed, WorldSaves.TryRenameWorld(_savesRoot, "before", "after"));
        Assert.False(WorldSaves.WorldExists(_savesRoot, "before"));
        Assert.True(WorldSaves.WorldExists(_savesRoot, "after"));
    }

    [Fact]
    public void RenamingSaysWhyItDidNothing()
    {
        Save("here");
        Save("taken");

        Assert.Equal(WorldRenameResult.SourceMissing, WorldSaves.TryRenameWorld(_savesRoot, "missing", "new"));
        Assert.Equal(WorldRenameResult.NameTaken, WorldSaves.TryRenameWorld(_savesRoot, "here", "taken"));
        Assert.Equal(WorldRenameResult.Unchanged, WorldSaves.TryRenameWorld(_savesRoot, "here", "here"));
    }

    [Fact]
    public void AWorldCanBeRecapitalised()
    {
        Save("lower");

        Assert.Equal(WorldRenameResult.Renamed, WorldSaves.TryRenameWorld(_savesRoot, "lower", "Lower"));
        Assert.Contains("Lower", WorldSaves.ListWorlds(_savesRoot));
    }

    [Fact]
    public void DeletingTakesTheWorldAndSaysWhetherThereWasOne()
    {
        Save("doomed");

        Assert.True(WorldSaves.TryDeleteWorld(_savesRoot, "doomed"));
        Assert.False(WorldSaves.WorldExists(_savesRoot, "doomed"));
        Assert.False(WorldSaves.TryDeleteWorld(_savesRoot, "doomed"));
    }

    [Fact]
    public void AChunkThatWasNeverSavedIsRegeneratedRatherThanRead()
    {
        using var storage = new WorldStorage(_savesRoot, "empty");

        Assert.Null(storage.TryLoadChunk(null!, 0, 0));
    }

    private void Save(string worldName)
    {
        using var storage = new WorldStorage(_savesRoot, worldName);
        storage.SaveMetadata(new WorldMetadata { Seed = 1 });
    }

    private void WriteLevelDat(string worldName, params string[] lines)
    {
        string directory = WorldSaves.GetWorldDirectory(_savesRoot, worldName);
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "level.dat"), lines);
    }
}

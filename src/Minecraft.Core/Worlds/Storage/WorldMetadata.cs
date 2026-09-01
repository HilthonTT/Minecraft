using Minecraft.Core.Games;

namespace Minecraft.Core.Worlds.Storage;

public sealed class WorldMetadata
{
    public const int CurrentVersion = 2;

    public int Version { get; init; } = CurrentVersion;

    public required int Seed { get; init; }

    public float CurrentTime { get; set; } = World.MiddayTimeSeconds;

    public GameMode GameMode { get; set; } = GameMode.Creative;
}

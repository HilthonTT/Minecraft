using Minecraft.Core;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Entities;

[Collection(RegistryCollection.Name)]
public sealed class PlayerDamageTests
{
    private static ServerPlayer Survivor()
    {
        var player = new ServerPlayer(1, "tester", null, Vector3.Zero);
        player.SetGameMode(GameMode.Survival);
        return player;
    }

    [Fact]
    public void ASmallerHitDuringTheHurtWindowIsIgnored()
    {
        ServerPlayer player = Survivor();

        Assert.True(player.TryHurt(5));
        Assert.False(player.TryHurt(3));

        Assert.Equal(Constants.PLAYER_MAX_HEALTH - 5, player.Health);
    }

    [Fact]
    public void ABiggerHitDuringTheHurtWindowLandsTheDifference()
    {
        ServerPlayer player = Survivor();

        Assert.True(player.TryHurt(3));
        Assert.True(player.TryHurt(Constants.PLAYER_MAX_HEALTH));

        Assert.Equal(0, player.Health);
        Assert.False(player.IsAlive);
    }

    [Fact]
    public void StarvationDoesNotFireWhileTheHurtWindowWouldSwallowIt()
    {
        ServerPlayer player = Survivor();
        player.AddExhaustion(Constants.PLAYER_EXHAUSTION_PER_FOOD * Constants.PLAYER_MAX_FOOD);
        player.TryDrainFood();

        player.TryHurt(1);

        Assert.False(player.TryStarve(Constants.PLAYER_STARVE_SECONDS_PER_HEALTH));
    }
}

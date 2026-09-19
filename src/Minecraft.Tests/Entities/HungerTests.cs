using Minecraft.Core;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories.Items;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Entities;

[Collection(RegistryCollection.Name)]
public sealed class HungerTests
{
    private static ServerPlayer Survivor()
    {
        var player = new ServerPlayer(1, "tester", null, Vector3.Zero);
        player.SetGameMode(GameMode.Survival);
        return player;
    }

    private static void Walk(ServerPlayer player, int blocks)
    {
        for (int step = 0; step < blocks; step++)
        {
            player.OnMoved(new Vector3(step, 0, 0), new Vector3(step + 1, 0, 0));
        }
    }

    private static void Starve(ServerPlayer player)
    {
        player.AddExhaustion(Constants.PLAYER_EXHAUSTION_PER_FOOD * Constants.PLAYER_MAX_FOOD);
        player.TryDrainFood();
    }

    [Fact]
    public void APlayerStartsFull()
    {
        Assert.Equal(Constants.PLAYER_MAX_FOOD, Survivor().Food);
    }

    [Fact]
    public void FourHundredBlocksOfWalkingCostsHalfAShank()
    {
        ServerPlayer player = Survivor();

        Walk(player, 399);
        Assert.False(player.TryDrainFood());

        Walk(player, 2);
        Assert.True(player.TryDrainFood());
        Assert.Equal(Constants.PLAYER_MAX_FOOD - 1, player.Food);
    }

    [Fact]
    public void SprintingIsTenTimesHungrier()
    {
        ServerPlayer player = Survivor();
        player.IsSprinting = true;

        Walk(player, 41);

        Assert.True(player.TryDrainFood());
    }

    [Fact]
    public void AJumpAcrossTheMapIsNotCountedAsAWalk()
    {
        ServerPlayer player = Survivor();

        player.OnMoved(Vector3.Zero, new Vector3(5000, 0, 0));

        Assert.Equal(0F, player.Exhaustion);
    }

    [Fact]
    public void CreativeNeverGetsHungry()
    {
        var player = new ServerPlayer(1, "tester", null, Vector3.Zero);
        player.SetGameMode(GameMode.Creative);

        Walk(player, 10000);

        Assert.False(player.TryDrainFood());
        Assert.Equal(Constants.PLAYER_MAX_FOOD, player.Food);
    }

    [Fact]
    public void EatingFillsTheBarUpToItsTop()
    {
        ServerPlayer player = Survivor();
        Starve(player);

        Assert.True(player.TryEat(ItemRegistry.Steak));
        Assert.Equal(ItemRegistry.Steak.Nourishment, player.Food);

        player.TryEat(ItemRegistry.Steak);
        player.TryEat(ItemRegistry.Steak);
        Assert.Equal(Constants.PLAYER_MAX_FOOD, player.Food);
    }

    [Fact]
    public void AFullPlayerCannotEat()
    {
        Assert.False(Survivor().TryEat(ItemRegistry.Bread));
    }

    [Fact]
    public void CookingIsWorthIt()
    {
        Assert.True(ItemRegistry.Steak.Nourishment > ItemRegistry.RawBeef.Nourishment);
        Assert.True(ItemRegistry.CookedPorkchop.Nourishment > ItemRegistry.RawPorkchop.Nourishment);
        Assert.True(ItemRegistry.CookedMutton.Nourishment > ItemRegistry.RawMutton.Nourishment);
    }

    [Fact]
    public void StarvationHurtsOnlyOnceTheBarIsEmpty()
    {
        ServerPlayer player = Survivor();

        Assert.False(player.TryStarve(Constants.PLAYER_STARVE_SECONDS_PER_HEALTH));

        Starve(player);

        Assert.Equal(0, player.Food);
        Assert.True(player.TryStarve(Constants.PLAYER_STARVE_SECONDS_PER_HEALTH));
    }

    [Fact]
    public void StarvationStopsShortOfKilling()
    {
        ServerPlayer player = Survivor();
        Starve(player);

        player.TryHurt(Constants.PLAYER_MAX_HEALTH - 1);

        Assert.Equal(1, player.Health);
        Assert.False(player.TryStarve(Constants.PLAYER_STARVE_SECONDS_PER_HEALTH));
    }

    [Fact]
    public void RespawningRefillsTheBar()
    {
        ServerPlayer player = Survivor();
        Starve(player);

        player.Respawn(Vector3.Zero);

        Assert.Equal(Constants.PLAYER_MAX_FOOD, player.Food);
    }
}

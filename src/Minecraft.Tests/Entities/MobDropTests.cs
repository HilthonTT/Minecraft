using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Entities;

[Collection(RegistryCollection.Name)]
public sealed class MobDropTests
{
    private static List<int> CountsOf(Mob mob, Item item)
    {
        var random = new Random(1234);
        List<int> counts = [];

        for (int roll = 0; roll < 200; roll++)
        {
            counts.Add(mob.RollDrops(random).Where(stack => stack.Item == item).Sum(stack => stack.Count));
        }

        return counts;
    }

    [Fact]
    public void ACowLeavesOneToThreeRawBeef()
    {
        List<int> counts = CountsOf(new Cow(1, null, Vector3.Zero), ItemRegistry.RawBeef);

        Assert.All(counts, count => Assert.InRange(count, 1, 3));
        Assert.Contains(3, counts);
    }

    [Fact]
    public void APigLeavesOneToThreeRawPorkchops()
    {
        List<int> counts = CountsOf(new Pig(1, null, Vector3.Zero), ItemRegistry.RawPorkchop);

        Assert.All(counts, count => Assert.InRange(count, 1, 3));
    }

    [Fact]
    public void ASheepAlwaysLeavesItsWool()
    {
        List<int> counts = CountsOf(new Sheep(1, null, Vector3.Zero), ItemRegistry.For(BlockRegistry.Wool));

        Assert.All(counts, count => Assert.Equal(1, count));
    }

    [Fact]
    public void AZombieSometimesLeavesNothing()
    {
        List<int> counts = CountsOf(new Zombie(1, null, Vector3.Zero), ItemRegistry.RottenFlesh);

        Assert.All(counts, count => Assert.InRange(count, 0, 2));
        Assert.Contains(0, counts);
    }
}

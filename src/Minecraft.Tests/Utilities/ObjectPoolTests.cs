using Minecraft.Core.Utilities;

namespace Minecraft.Tests.Utilities;

public sealed class ObjectPoolTests
{
    private sealed class Pooled
    {
    }

    [Fact]
    public void AnItemThatIsNotLentOutHasNoLease()
    {
        var pool = new ObjectPool<Pooled>(1);
        Pooled item = pool.GetObject();
        pool.ReturnObject(item);

        Assert.Equal(0, pool.LeaseOf(item));
    }

    [Fact]
    public void LendingTheSameItemAgainGivesItANewLease()
    {
        var pool = new ObjectPool<Pooled>(1);

        Pooled first = pool.GetObject();
        long firstLease = pool.LeaseOf(first);
        pool.ReturnObject(first);

        Pooled second = pool.GetObject();

        Assert.Same(first, second);
        Assert.NotEqual(0, firstLease);
        Assert.NotEqual(firstLease, pool.LeaseOf(second));
    }
}

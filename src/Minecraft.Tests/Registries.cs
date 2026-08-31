using Minecraft.Core.Inventories.Crafting;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Tests;

/// <summary>
/// Brings up the three static registries the way the game does, in the one order they can be built in:
/// blocks first, then an item for every one of them, then a recipe book written in terms of both.
/// <para>
/// They are static and global, so this happens once for the whole test run rather than per test. Touching
/// <see cref="Ready"/> from a fixture is what forces it; the class initialiser does the rest, and the runtime
/// guarantees it runs exactly once however many threads arrive at it together.
/// </para>
/// </summary>
public static class Registries
{
    static Registries()
    {
        BlockRegistry.RegisterBlocks();
        ItemRegistry.RegisterItems();
        RecipeRegistry.RegisterRecipes();
    }

    public static bool Ready => true;
}

/// <summary>
/// Applied to every test class that reaches for a block, an item or a recipe. xUnit builds one fixture per
/// collection, so this is the hook that runs <see cref="Registries"/> before any of them.
/// </summary>
public sealed class RegistryFixture
{
    public RegistryFixture() => _ = Registries.Ready;
}

[CollectionDefinition(Name)]
public sealed class RegistryCollection : ICollectionFixture<RegistryFixture>
{
    public const string Name = "registries";
}

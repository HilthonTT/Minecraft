using Minecraft.Core.Inventories.Crafting;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Tests;

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

public sealed class RegistryFixture
{
    public RegistryFixture() => _ = Registries.Ready;
}

[CollectionDefinition(Name)]
public sealed class RegistryCollection : ICollectionFixture<RegistryFixture>
{
    public const string Name = "registries";
}

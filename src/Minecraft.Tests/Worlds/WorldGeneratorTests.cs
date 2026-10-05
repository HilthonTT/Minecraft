using System.Diagnostics;
using Minecraft.Core.Worlds.Generation;
using OpenTK.Mathematics;

namespace Minecraft.Tests.Worlds;

[Collection(RegistryCollection.Name)]
public sealed class WorldGeneratorTests
{
    [Fact]
    public void AStoppedGeneratorLetsGoOfItsThreadAndTakesNoMoreWork()
    {
        var generator = new WorldGenerator(null!, null!, seed: 1);

        var stopwatch = Stopwatch.StartNew();
        generator.Dispose();
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1));

        bool served = false;
        generator.AddChunkGenerationRequest(new GenerateChunkRequest
        {
            GridPosition = new Vector2(0, 0),
            Callback = _ => served = true,
        });

        Thread.Sleep(50);

        Assert.False(served);

        generator.Dispose();
    }
}

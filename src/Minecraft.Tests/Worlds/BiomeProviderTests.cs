using Minecraft.Core.Worlds.Biomes;
using Minecraft.Core.Worlds.Generation;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Tests.Worlds;

[Collection(RegistryCollection.Name)]
public sealed class BiomeProviderTests
{
    [Fact]
    public void AnAnswerIsNotRewrittenByTheNextQuestion()
    {
        var provider = new BiomeProvider([new PlainsBiome(), new DesertBiome(), new SnowyPlainsBiome()]);

        BiomeMembership[] first = provider.GetBiomeMemberships(temperature: 0.1, moisture: 0.1);
        double[] firstShares = first.Select(membership => membership.Percentage).ToArray();

        provider.GetBiomeMemberships(temperature: 0.9, moisture: 0.9);

        Assert.Equal(firstShares, first.Select(membership => membership.Percentage));
    }

    [Fact]
    public void TerrainSampledOnTwoThreadsAtOnceMatchesTerrainSampledOnOne()
    {
        Biome[] biomes =
        [
            new PlainsBiome(),
            new ForestBiome(),
            new DesertBiome(),
            new MountainBiome(),
            new SnowyPlainsBiome(),
        ];

        var sampler = new TerrainSampler(biomes, seaLevel: 62, lowestSurface: 8);

        const int columns = 4000;
        var expected = new TerrainColumn[columns];
        for (int i = 0; i < columns; i++)
        {
            expected[i] = sampler.SampleColumn(i * 7, i * 13);
        }

        var onThreads = new TerrainColumn[2][];
        using var start = new Barrier(onThreads.Length);

        Thread[] threads = Enumerable.Range(0, onThreads.Length)
            .Select(thread => new Thread(() =>
            {
                var sampled = new TerrainColumn[columns];
                start.SignalAndWait();

                for (int i = 0; i < columns; i++)
                {
                    sampled[i] = sampler.SampleColumn(i * 7, i * 13);
                }

                onThreads[thread] = sampled;
            }))
            .ToArray();

        foreach (Thread thread in threads)
        {
            thread.Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        Assert.Equal(expected, onThreads[0]);
        Assert.Equal(expected, onThreads[1]);
    }
}

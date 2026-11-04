namespace Minecraft.Core.Worlds.Biomes;

public sealed class BiomeProvider
{
    private const double MinimumClimateDistance = 1e-6;

    private readonly Biome[] _registeredBiomes;

    public BiomeProvider(Biome[] registeredBiomes)
    {
        _registeredBiomes = registeredBiomes;
    }

    public BiomeMembership[] GetBiomeMemberships(double temperature, double moisture)
    {
        var memberships = new BiomeMembership[_registeredBiomes.Length];

        double sum = 0;
        for (int i = 0; i < _registeredBiomes.Length; i++)
        {
            double temperatureDistance = Math.Abs(_registeredBiomes[i].Temperature - temperature);
            double moistureDistance = Math.Abs(_registeredBiomes[i].Moisture - moisture);
            double distance = Math.Max(temperatureDistance + moistureDistance, MinimumClimateDistance);

            double weight = 1 / (distance * distance * distance);
            memberships[i] = new BiomeMembership
            {
                Percentage = weight,
                Biome = _registeredBiomes[i],
            };
            sum += weight;
        }

        for (int i = 0; i < memberships.Length; i++)
        {
            memberships[i].Percentage /= sum;
        }

        return memberships;
    }
}
